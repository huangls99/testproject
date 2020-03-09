using Kingdee.BOS;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace MaFeeg.K3Cloud.Developments
{
    [Description("审核方案生成销售订单")]
    [Kingdee.BOS.Util.HotUpdate]
    public class PreSaleOrder : AbstractOperationServicePlugIn
    {

        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            try
            {
                ReturnParam param = new ReturnParam();
                string sql = "";
                for (int i = 0; i < e.DataEntitys.Length; i++)
                {
                    //获取单据体数据包
                    DynamicObject Item = e.DataEntitys[i];
                    if (!string.IsNullOrEmpty(Item.ToString()))
                    {
                        //表单id
                        string FID = Item["Id"].ToString();
                        //表头
                        sql = "select * from  t_OrderPlan where FID=" + FID + "";
                        //先进先出方案
                        sql += "select * from  t_OrderPlanEntry where FID=" + FID + " and F_PAEZ_CHECKBOX=1";
                        //按右最优
                        sql += "select * from  t_OrderPlanEntry2 where FID=" + FID + " and F_PAEZ_CHECKBOX1=1";
                        //按左最优
                        sql += "select * from  t_OrderPlanEntry3 where FID=" + FID + " and F_PAEZ_CHECKBOX2=1";
                        DataSet ds = DBServiceHelper.ExecuteDataSet(this.Context, sql);
                        DataTable dt = ds.Tables[0];
                        DataTable dt1= ds.Tables[1];
                        DataTable dt2 = ds.Tables[2];
                        DataTable dt3 = ds.Tables[3];
                        if (dt1.Rows.Count > 0&& dt2.Rows.Count>0|| dt1.Rows.Count > 0 && dt3.Rows.Count > 0|| dt2.Rows.Count > 0 && dt3.Rows.Count>0)
                        {
                            throw new KDException("", "不能同时选择两种下单方案！");
                        }
                        if (dt1.Rows.Count>0)
                        {
                            param = SaleOrder(dt, dt1 ,1);
                        }
                        if (dt2.Rows.Count > 0)
                        {
                            param = SaleOrder(dt, dt2,2);
                        }
                        if (dt3.Rows.Count > 0)
                        {
                            param = SaleOrder(dt, dt3,3);
                        }
                        if(dt1.Rows.Count == 0 && dt2.Rows.Count==0&& dt3.Rows.Count == 0)
                        {
                            throw new KDException("", "未选择方案数据");

                        }
                        if (!param.status)
                        {
                            string msg = param.msg; ;
                            throw new KDException("生成销售订单失败：", msg);

                        }

                    }

                }
             
            }
            catch (Exception ex)
            {
                string msg = ex.ToString();
                throw new KDException("生成销售订单失败：", msg);

            }
        }


        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            

        }
       
        /// <summary>
        /// 组装销售订单数据并保存
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public ReturnParam SaleOrder(DataTable dt,DataTable dtbt,int a)
      {
            ReturnParam param = new ReturnParam();
            #region 方法二： 创建视图、模型，模拟手工新增，会触发大部分的表单服务和插件
            FormMetadata meta = MetaDataServiceHelper.Load(this.Context, "SAL_SaleOrder") as FormMetadata;
            BusinessInfo info = meta.BusinessInfo;
            IResourceServiceProvider formServiceProvider = meta.BusinessInfo.GetForm().GetFormServiceProvider(true);
            IBillViewService billViewService = formServiceProvider.GetService(typeof(IDynamicFormView)) as IBillViewService;

            /******创建单据打开参数*************/
            Form form = meta.BusinessInfo.GetForm();
            BillOpenParameter billOpenParameter = new BillOpenParameter(form.Id, meta.GetLayoutInfo().Id);
            billOpenParameter = new BillOpenParameter(form.Id, string.Empty);
            billOpenParameter.Context = Context;
            billOpenParameter.ServiceName = form.FormServiceName;
            billOpenParameter.PageId = Guid.NewGuid().ToString();
            billOpenParameter.FormMetaData = meta;
            billOpenParameter.LayoutId = meta.GetLayoutInfo().Id;
            billOpenParameter.Status = OperationStatus.ADDNEW;
            billOpenParameter.PkValue = null;
            billOpenParameter.CreateFrom = CreateFrom.Default;
            billOpenParameter.ParentId = 0;
            billOpenParameter.GroupId = "";
            billOpenParameter.DefaultBillTypeId = null;
            billOpenParameter.DefaultBusinessFlowId = null;
            billOpenParameter.SetCustomParameter("ShowConfirmDialogWhenChangeOrg", false);
            List<AbstractDynamicFormPlugIn> value = form.CreateFormPlugIns();
            billOpenParameter.SetCustomParameter(FormConst.PlugIns, value);

            ((IDynamicFormViewService)billViewService).Initialize(billOpenParameter, formServiceProvider);

            IBillView bill_view = (IBillView)billViewService;

            bill_view.CreateNewModelData();

            DynamicFormViewPlugInProxy proxy = bill_view.GetService<DynamicFormViewPlugInProxy>();
            proxy.FireOnLoad();
            string FID = string.Empty;
            string updatesql = string.Empty;
            updatesql = "";
            List<string> listsql = new List<string>();
            string FProgramCode = "";
            //表头
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                FID = dt.Rows[i]["FID"].ToString();
                FProgramCode= dt.Rows[i]["FBILLNO"].ToString();
                bill_view.Model.SetItemValueByID("FSaleOrgId", dt.Rows[i]["FSALEORGID"], 0);
                bill_view.Model.SetValue("FDate", DateTime.Now.ToString());
                //客户
                bill_view.Model.SetItemValueByID("FCustId", dt.Rows[i]["FCUSTID"].ToString(), 0);
                //结算币别
                bill_view.InvokeFieldUpdateService("FCustId", 0);
                bill_view.InvokeFieldUpdateService("FSETTLECURRID", 0);
                //销售员
                bill_view.Model.SetValue("FSalerId", dt.Rows[i]["FSALERID"].ToString(), 0);
                bill_view.InvokeFieldUpdateService("FSALEDEPTID", 0);
                //交货方式
                bill_view.Model.SetValue("FHeadDeliveryWay", dt.Rows[i]["FHEADDELIVERYWAY"].ToString());
                //交货地点
                bill_view.Model.SetValue("FHEADLOCID", dt.Rows[i]["FHEADLOCID"].ToString());
            }
            if (a == 1)
            {

                //表体
                for (int i = 0; i < dtbt.Rows.Count; i++)
                {
                    bill_view.Model.CreateNewEntryRow("FSaleOrderEntry");
                    bill_view.Model.SetValue("FMaterialId", dtbt.Rows[i]["FMATERIALID"].ToString(), i);
                    bill_view.InvokeFieldUpdateService("FMATERIALID", i);
                    bill_view.InvokeFieldUpdateService("FUNITID", i);
                    bill_view.InvokeFieldUpdateService("FBASEUNITID", i);
                    // 销售数量
                    bill_view.Model.SetValue("FQty", Convert.ToDouble(dtbt.Rows[i]["FQTY"].ToString()), i);
                    bill_view.InvokeFieldUpdateService("FQty", i); //
                    bill_view.InvokeFieldUpdateService("FPriceUnitQty", i); //计家数量
                    //含税单价
                    bill_view.Model.SetValue("FTaxPrice", 20, i);
                    bill_view.InvokeFieldUpdateService("FTaxPrice", i);
                    bill_view.InvokeFieldUpdateService("FPrice", i);
                    bill_view.InvokeFieldUpdateService("FAmount", i);
                    bill_view.InvokeFieldUpdateService("FAllAmount", i);
                    bill_view.InvokeFieldUpdateService("FAllAmount", i);
                    //要货日期DateTime.Now.AddDays(10
                    bill_view.Model.SetValue("FDeliveryDate", DateTime.Now.AddDays(10), i);
                    //计划交货日期
                    bill_view.Model.SetValue("FMinPlanDeliveryDate", DateTime.Now.AddDays(10), i);
                    //结算组织
                    bill_view.Model.SetItemValueByID("FSettleOrgIds", this.Context.CurrentOrganizationInfo.ID, i);
                    //批号FLot
                    bill_view.Model.SetValue("FLot", dtbt.Rows[i]["FLOT"].ToString(), i);
                    // bill_view.Model.SetValue("FLOT_TEXT", dtbt.Rows[i]["FINVOICE"].ToString() + "_" + dtbt.Rows[i]["FBOARDNO"].ToString() + "_" + dtbt.Rows[i]["FCARTONNO"].ToString(), i);
                    int FSEQ = i + 1;
                    updatesql = "update T_SAL_ORDERENTRY set FAUXPROPID='" + dtbt.Rows[i]["FAUXPROPID"].ToString() + "',FLOT_TEXT='" + dtbt.Rows[i]["FINVOICE"].ToString() + "_" + dtbt.Rows[i]["FBOARDNO"].ToString() + "_" + dtbt.Rows[i]["FCARTONNO"].ToString() + "',FLOT=" + dtbt.Rows[i]["FLOT"].ToString() + " where FSEQ=" + FSEQ + "";
                    listsql.Add(updatesql);

                }

            }
            else if (a == 2)
            {
                //表体
                for (int i = 0; i < dtbt.Rows.Count; i++)
                {
                    bill_view.Model.CreateNewEntryRow("FSaleOrderEntry");
                    bill_view.Model.SetValue("FMaterialId", dtbt.Rows[i]["FMATERIALID2"].ToString(), i);
                    bill_view.InvokeFieldUpdateService("FMATERIALID", i);
                    bill_view.InvokeFieldUpdateService("FUNITID", i);
                    bill_view.InvokeFieldUpdateService("FBASEUNITID", i);
                    // 销售数量
                    bill_view.Model.SetValue("FQty", Convert.ToDouble(dtbt.Rows[i]["FQTY2"].ToString()), i);
                    bill_view.InvokeFieldUpdateService("FQty", i); //
                    bill_view.InvokeFieldUpdateService("FPriceUnitQty", i); //计家数量
                                                                            //含税单价
                    bill_view.Model.SetValue("FTaxPrice", 20, i);
                    bill_view.InvokeFieldUpdateService("FTaxPrice", i);
                    bill_view.InvokeFieldUpdateService("FPrice", i);
                    bill_view.InvokeFieldUpdateService("FAmount", i);
                    bill_view.InvokeFieldUpdateService("FAllAmount", i);
                    bill_view.InvokeFieldUpdateService("FAllAmount", i);
                    //要货日期DateTime.Now.AddDays(10
                    bill_view.Model.SetValue("FDeliveryDate", DateTime.Now.AddDays(10), i);
                    //计划交货日期
                    bill_view.Model.SetValue("FMinPlanDeliveryDate", DateTime.Now.AddDays(10), i);
                    //结算组织
                    bill_view.Model.SetItemValueByID("FSettleOrgIds", this.Context.CurrentOrganizationInfo.ID, i);
                    //批号FLot
                    bill_view.Model.SetValue("FLot", dtbt.Rows[i]["FLOT2"].ToString(), i);
                   // bill_view.Model.SetValue("FLOT_TEXT", dtbt.Rows[i]["FINVOICE2"].ToString() + "_" + dtbt.Rows[i]["FBOARDNO2"].ToString() + "_" + dtbt.Rows[i]["FCARTONNO2"].ToString(), i);
                     int FSEQ = i + 1;
                    updatesql = "update T_SAL_ORDERENTRY set FAUXPROPID='"+ dtbt.Rows[i]["FAUXPROPID2"].ToString() + "', FLOT_TEXT='" + dtbt.Rows[i]["FINVOICE2"].ToString() + "_" + dtbt.Rows[i]["FBOARDNO2"].ToString() + "_" + dtbt.Rows[i]["FCARTONNO2"].ToString() + "',FLOT =" + dtbt.Rows[i]["FLOT2"].ToString() + " where FSEQ=" + FSEQ + "";
                    listsql.Add(updatesql);

                }

            }
            else if (a == 3)
            {
                //表体
                for (int i = 0; i < dtbt.Rows.Count; i++)
                {
                    bill_view.Model.CreateNewEntryRow("FSaleOrderEntry");
                    bill_view.Model.SetValue("FMaterialId", dtbt.Rows[i]["FMATERIALID3"].ToString(), i);
                    bill_view.InvokeFieldUpdateService("FMATERIALID", i);
                    bill_view.InvokeFieldUpdateService("FUNITID", i);
                    bill_view.InvokeFieldUpdateService("FBASEUNITID", i);
                    // 销售数量
                    bill_view.Model.SetValue("FQty", Convert.ToDouble(dtbt.Rows[i]["FQTY3"].ToString()), i);
                    bill_view.InvokeFieldUpdateService("FQty", i); //
                    bill_view.InvokeFieldUpdateService("FPriceUnitQty", i); //计家数量
                                                                            //含税单价
                    bill_view.Model.SetValue("FTaxPrice", 20, i);
                    bill_view.InvokeFieldUpdateService("FTaxPrice", i);
                    bill_view.InvokeFieldUpdateService("FPrice", i);
                    bill_view.InvokeFieldUpdateService("FAmount", i);
                    bill_view.InvokeFieldUpdateService("FAllAmount", i);
                    bill_view.InvokeFieldUpdateService("FAllAmount", i);
                    //要货日期DateTime.Now.AddDays(10
                    bill_view.Model.SetValue("FDeliveryDate", DateTime.Now.AddDays(10), i);
                    //计划交货日期
                    bill_view.Model.SetValue("FMinPlanDeliveryDate", DateTime.Now.AddDays(10), i);
                    //结算组织
                    bill_view.Model.SetItemValueByID("FSettleOrgIds", this.Context.CurrentOrganizationInfo.ID, i);
                    //批号FLot
                    bill_view.Model.SetValue("FLot", dtbt.Rows[i]["FLOT3"].ToString(), i);
                   // bill_view.Model.SetValue("FLOT_TEXT", dtbt.Rows[i]["FINVOICE3"].ToString()+"_"+ dtbt.Rows[i]["FBOARDNO3"].ToString()+"_"+ dtbt.Rows[i]["FCARTONNO3"].ToString(), i);
                    int FSEQ = i + 1;
                    updatesql = "update T_SAL_ORDERENTRY set FAUXPROPID='" + dtbt.Rows[i]["FAUXPROPID3"].ToString() + "', FLOT_TEXT='" + dtbt.Rows[i]["FINVOICE3"].ToString() + "_" + dtbt.Rows[i]["FBOARDNO3"].ToString() + "_" + dtbt.Rows[i]["FCARTONNO3"].ToString() + "',FLOT=" + dtbt.Rows[i]["FLOT3"].ToString() + " where FSEQ=" + FSEQ + "";
                    listsql.Add(updatesql);

                }

            }
            string result = "";
            //保存
            IOperationResult save_result = bill_view.Model.Save();
            if (save_result.IsSuccess)
            {
                string fid = string.Empty;
                string Fnumber = string.Empty;
                OperateResultCollection Collection = save_result.OperateResult;
                foreach (var item in Collection)
                {
                    fid = item.PKValue.ToString();
                    Fnumber = item.Number.ToString();
                }
                //更新到方案FSalesOrder
                string sql = "update t_OrderPlan set FSalesOrder='"+ Fnumber + "'  where  FID="+ FID + ";";
                sql += " update  T_SAL_ORDER set FProgramCode='"+ FProgramCode + "'  where  FID="+ fid + ";";
                DBServiceHelper.Execute(this.Context, sql);
                sql = " and FID=" + fid + " ;";
                foreach (var item in listsql)
                {
                    string upsql = item + sql;
                    DBServiceHelper.Execute(this.Context, upsql);

                }
                param.msg = "生成销售订单成功:"+ Fnumber;
                param.status = true;
                return param;
            }
            else
            {
                for (int mf = 0; mf < save_result.ValidationErrors.Count; mf++)
                {
                    result += "\r\n" + save_result.ValidationErrors[mf].Message;
                }

                param.msg = "保存失败："+ result;
                param.status = false;
                return param;
            }
            #endregion
      }
    }
}
