using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Kingdee.BOS;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.BusinessEntity;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.App;
using Kingdee.BOS.Core.Metadata.FieldElement;


namespace MaFeeg.K3Cloud.Developments
{
     
    public class CreateLinkEntryForImport : AbstractOperationServicePlugIn
    {
        private const string POFormId = "PUR_PurchaseOrder";
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);
            HashSet<string> poBillNos = new HashSet<string>();
            Entity entity = this.BusinessInfo.GetEntity("FInStockEntry");
            Entity linkEntry = this.BusinessInfo.GetEntity("FInStockEntry_Link");
            Field fldSrcFormId = this.BusinessInfo.GetField("FSRCBILLTYPEID");
            Field fldSrcBillNo = this.BusinessInfo.GetField("FSrcBillNo");
            // 对单据体进行循环，取关联的源单编号
            foreach (var billObj in e.SelectedRows)
            {
                DynamicObjectCollection entryRows = entity.DynamicProperty.GetValue(billObj.DataEntity)
                            as DynamicObjectCollection;
                foreach (var entryRow in entryRows)
                {
                    string srcFormId = fldSrcFormId.DynamicProperty.GetValue<string>(entryRow);
                    string srcSrcBillNo = fldSrcBillNo.DynamicProperty.GetValue<string>(entryRow);
                    if (string.IsNullOrWhiteSpace(srcFormId)|| !srcFormId.EqualsIgnoreCase(POFormId))
                    {// 源单不是采购订单，略过
                        continue;
                    }
                    // 源单编号已经登记，不再重复记录，略过
                    if (poBillNos.Contains(srcSrcBillNo)) continue;
                    // Link已经记录了源单信息，略过
                    DynamicObjectCollection linkRows = linkEntry.DynamicProperty.GetValue(entryRow)
                            as DynamicObjectCollection;
                    if (linkRows.Count > 0) continue;
                    poBillNos.Add(srcSrcBillNo);
                }
            }
            if (poBillNos.Count == 0) return;
            DynamicObject[] poObjs = this.LoadPurchaseOrder(poBillNos);
            if (poObjs == null || poObjs.Length == 0) return;
            Dictionary<string, Dictionary<string, DynamicObject>> dctAllBills = this.BuildDictionary(poObjs);
            string srcTableNumber = this.GetPOEntryTableNumber();
            List<DynamicObject> allNewLinkRows = new List<DynamicObject>();
            // 循环单据体，为单据体，建立起源单关联信息：
            foreach (var billObj in e.SelectedRows)
            {
                DynamicObjectCollection entryRows = entity.DynamicProperty.GetValue(billObj.DataEntity)
                            as DynamicObjectCollection;
                foreach (var entryRow in entryRows)
                {
                    string srcFormId = fldSrcFormId.DynamicProperty.GetValue<string>(entryRow);
                    string srcSrcBillNo = fldSrcBillNo.DynamicProperty.GetValue<string>(entryRow);
                    if (string.IsNullOrWhiteSpace(srcFormId)|| !srcFormId.EqualsIgnoreCase(POFormId))
                    {// 源单不是采购订单，略过
                        continue;
                    }
                    Dictionary<string, DynamicObject> dctOneBill = null;
                    if (dctAllBills.TryGetValue(srcSrcBillNo, out dctOneBill) == false) continue;
                    DynamicObject materialObj = entryRow["MaterialId"] as DynamicObject;
                    if (materialObj == null) continue;
                    string materialNumber = Convert.ToString(materialObj["number"]);
                    DynamicObject srcRow = null;
                    if (dctOneBill.TryGetValue(materialNumber, out srcRow) == false) continue;
                    // Link已经记录了源单信息，略过
                    DynamicObjectCollection linkRows = linkEntry.DynamicProperty.GetValue(entryRow) as DynamicObjectCollection;
                    if (linkRows.Count > 0) continue;
                    DynamicObject linkRow = new DynamicObject(linkEntry.DynamicObjectType);
                    linkRow["STableName"] = srcTableNumber;
                    this.FillLinkRow(srcRow, entryRow, linkRow);
                    linkRows.Add(linkRow);
                    allNewLinkRows.Add(linkRow);
                }
            }
            // 为新建的源单关联信息，设置内码
            IDBService dbService = ServiceHelper.GetService<IDBService>();
            dbService.AutoSetPrimaryKey(this.Context, allNewLinkRows.ToArray(), linkEntry.DynamicObjectType);
        }

        private DynamicObject[] LoadPurchaseOrder(HashSet<string> poBillNos)
        {
            IViewService viewService = ServiceHelper.GetService<IViewService>();
            string formId = "PUR_PurchaseOrder";

            // 指定需要加载的采购订单字段
            List<SelectorItemInfo> fields = new List<SelectorItemInfo>();
            fields.Add(new SelectorItemInfo("FID"));        // 单据主键
            fields.Add(new SelectorItemInfo("FPOOrderEntry_FEntryID"));     // 单据体主键
            fields.Add(new SelectorItemInfo("FBillNo"));    // 单据编号
            fields.Add(new SelectorItemInfo("FBFLowId"));    // 业务流程
            fields.Add(new SelectorItemInfo("FMaterialId"));    // 物料
            fields.Add(new SelectorItemInfo(" FBaseUnitQty"));           // 基本单位数量
            fields.Add(new SelectorItemInfo("FBaseJoinQty"));           // 基本单位关联数量
            // 指定过滤条件
            string filter = string.Format(" FBillNo IN ('{0}') ", string.Join("','", poBillNos));
            OQLFilter ofilter = OQLFilter.CreateHeadEntityFilter(filter);
            var objs = viewService.Load(this.Context, formId, fields, ofilter);
            return objs;
        }
        
        private Dictionary<string, Dictionary<string, DynamicObject>> BuildDictionary(DynamicObject[] poObjs)
        {
            Dictionary<string, Dictionary<string, DynamicObject>> dctAllBills =
                new Dictionary<string, Dictionary<string, DynamicObject>>();
            foreach (var poObj in poObjs)
            {
                string billNo = Convert.ToString(poObj["BillNo"]);
                Dictionary<string, DynamicObject> dctOneBill = new Dictionary<string, DynamicObject>();
                DynamicObjectCollection entryRows = poObj["POOrderEntry"] as DynamicObjectCollection;
                foreach (var entryRow in entryRows)
                {
                    DynamicObject materialObj = entryRow["MaterialId"] as DynamicObject;
                    if (materialObj == null) continue;
                    string materialNumber = Convert.ToString(materialObj["number"]);
                    dctOneBill[materialNumber] = entryRow;
                }
                dctAllBills.Add(billNo, dctOneBill);
            }
            return dctAllBills;
        }
        
        private string GetPOEntryTableNumber()
        {
            IBusinessFlowService bfMetaService = ServiceHelper.GetService<IBusinessFlowService>();
            var tableDefine = bfMetaService.LoadTableDefine(this.Context, POFormId, "FPOOrderEntry");
            return tableDefine.TableNumber;
        }
        
        private void FillLinkRow(DynamicObject srcRow, DynamicObject toRow, DynamicObject linkRow)
        {
            linkRow["FlowId"] = srcRow["FBFLowId_Id"];
            linkRow["FlowLineId"] = 0;
            linkRow["RuleId"] = "PUR_PurchaseOrder-STK_InStock";
            linkRow["SBillId"] = ((DynamicObject)srcRow.Parent)[0];
            linkRow["SId"] = srcRow[0];
            // 原始携带量
            decimal baseUnitQty = Convert.ToDecimal(srcRow["BaseUnitQty"]);
            decimal joinUnitQty = Convert.ToDecimal(srcRow["BaseJoinQty"]);
            linkRow["BaseUnitQtyOld"] = baseUnitQty - joinUnitQty;
            linkRow["BaseUnitQty"] = toRow["BaseUnitQty"];
        }
    }
}
