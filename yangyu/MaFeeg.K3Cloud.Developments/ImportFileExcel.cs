using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using System.Data;

using Kingdee.BOS.Util;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.BusinessEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Model;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.ServiceHelper.Excel;
using System.ComponentModel;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Orm;

namespace MaFeeg.K3Cloud.Developments
{
    [Description("excel文件导入")]
    [Kingdee.BOS.Util.HotUpdate]
    public class ImportFileExcel : AbstractDynamicFormPlugIn
    {
        
        private List<string> FileNameList = new List<string> { };
        private List<string> _FileList = new List<string> { };
        private bool Import = false;
        public override void AfterBindData(EventArgs e)
        {
            this.View.GetControl("F_JD_BTNOK").Enabled = false;
            string CustomKey = this.View.OpenParameter.GetCustomParameter("CustomKey").ToString();//获取父级页面传参的参数

            if (CustomKey == "3001")
            {
                LocaleValue str = new LocaleValue("「Excel」引入");
                this.View.SetFormTitle(str);
            }
        }
        
        public override void CustomEvents(CustomEventsArgs e)
        {
            if (e.Key.EqualsIgnoreCase("F_JD_FileUpdate"))
            {
                this.View.GetControl("F_JD_FileUpdate").SetCustomPropertyValue("NeedCallback", true);
                this.View.GetControl("F_JD_FileUpdate").SetCustomPropertyValue("IsRequesting", false);
                if (e.EventName.EqualsIgnoreCase("FileChanged"))
                {
                    FileNameList.Clear();
                    _FileList.Clear();

                    // 文件上传完毕
                    // 取文件上传参数，文件名
                    JSONObject uploadInfo = KDObjectConverter.DeserializeObject<JSONObject>(e.EventArgs);
                    if (uploadInfo != null)
                    {
                        JSONArray json = new JSONArray(uploadInfo["NewValue"].ToString());
                        if (json.Count < 1)
                        {
                            // 锁定确定按钮
                            this.View.GetControl("F_JD_BTNOK").Enabled = false;
                        }
                        else
                        {
                            // 取上传的文件名
                            for (int i = 0; i < json.Count; i++)
                            {
                                string fileName = (json[i] as Dictionary<string, object>)["ServerFileName"].ToString();
                                string _FileName = (json[i] as Dictionary<string, object>)["FileName"].ToString();
                                FileNameList.Add(this.GetFullFileName(fileName));
                                _FileList.Add(_FileName);
                            }

                            this.View.GetControl("F_JD_BTNOK").Enabled = true;// 解锁确定按钮
                        }
                    }
                }
            }
        }

        
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            if (e.Key.EqualsIgnoreCase("F_JD_BTNOK"))
            {
                this.View.GetControl("F_JD_BTNOK").Enabled = false;
                if (FileNameList.Count < 1)
                {
                    this.View.ShowMessage("未检测到需要引入的excel文件！", MessageBoxType.Error);
                }
                else
                {
                    string _sql = @"select tt.FSUPPLIERID,FName,t1.FUSEORGID from t_BD_Supplier_L tt
                    left join t_BD_Supplier t1 on tt.FSUPPLIERID=t1.FSUPPLIERID
                    where FFORBIDSTATUS='A' and FDOCUMENTSTATUS='C'";

                    _sql += Environment.NewLine +
                    @"select * from [dbo].[T_BAS_SYSTEMPROFILE] where FCATEGORY='STK' and FKEY='STARTSTOCKDATE'";

                    _sql += Environment.NewLine +
                    @"SELECT t2.FSTOCKID,t0.FMASTERID,t0.FNUMBER,FBASEUNITID,t2.FBOXSTANDARDQTY,t0.FDXQTY,t0.FPCSCONVERT  FROM T_BD_MATERIAL t0 
                    LEFT OUTER JOIN t_BD_MaterialBase t1 ON t0.FMATERIALID = t1.FMATERIALID 
                    LEFT OUTER JOIN t_BD_MaterialStock t2 ON t0.FMATERIALID = t2.FMATERIALID 
                    LEFT OUTER JOIN T_BD_MATERIAL_L t0_L ON (t0.FMATERIALID = t0_L.FMATERIALID AND t0_L.FLocaleId = 2052) 
                    WHERE t0.FFORBIDSTATUS = 'A' AND t0.FUSEORGID=1 and t0.FDOCUMENTSTATUS ='C' and FUseOrgID=" + this.Context.CurrentOrganizationInfo.ID + " OPTION ( MAXDOP 0)";

                    _sql += Environment.NewLine +
                    @"SELECT FNumber FROM T_BAS_ASSISTANTDATAENTRY t0 
                    LEFT OUTER JOIN T_BAS_ASSISTANTDATAENTRY_L t0_L ON (t0.FENTRYID = t0_L.FENTRYID AND t0_L.FLocaleId = 2052) 
                    WHERE ((((t0.FFORBIDSTATUS <> 'B') AND (t0.FID = '5e52580cf85a8e' AND FDOCUMENTSTATUS = 'C')) 
                    AND (t0.FID = '5e52580cf85a8e' AND fdocumentstatus = 'C')) AND t0.FFORBIDSTATUS = 'A')";

                    DataSet ds = DBServiceHelper.ExecuteDataSet(this.Context, _sql);
                    DataTable dt_Supplier = ds.Tables[0];
                    DataTable dt_system = ds.Tables[1]; dt_system.PrimaryKey = new DataColumn[] { dt_system.Columns["FORGID"] };
                    DataTable dt_Item = ds.Tables[2]; dt_Item.PrimaryKey = new DataColumn[] { dt_Item.Columns["FNUMBER"] };
                    DataTable dt_Aux = ds.Tables[3]; dt_Aux.PrimaryKey = new DataColumn[] { dt_Aux.Columns["FNumber"] };

                    DataRow dr_sys = dt_system.Rows.Find(this.Context.CurrentOrganizationInfo.ID);
                    if (dr_sys == null)
                    {
                        this.View.ShowMessage("当前组织“" + this.Context.CurrentOrganizationInfo.Name + "”未启用库存组织。", MessageBoxType.Advise);
                        this._FileList.Clear();
                        this.FileNameList.Clear();
                        this.View.GetControl("F_JD_FileUpdate").SetValue(DBNull.Value);
                        this.View.Refresh();
                        return;
                    }

                    DataSet dss_1;
                    DataSet dss_2;
                    DataSet dss_3;
                    DataSet dss_4;
                    //手机—沃格与帝晶excel 格式
                    using (ExcelOperation helper = new ExcelOperation(this.View))
                    {
                        dss_1 = helper.ReadFromFile(FileNameList[0], 0, 0);
                        dss_2 = helper.ReadFromFile(FileNameList[0], 4, 0);
                        dss_3 = helper.ReadFromFile(FileNameList[0], 12, 0);
                        dss_4 = helper.ReadFromFile(FileNameList[0], 9, 0);
                    }


                    DataTable dt_PL = dss_4.Tables[1];

                    DataTable dt_INV = dss_2.Tables["INV"];
                    DataTable dt_Price = dss_3.Tables["INV"];

                    string FULLNAME = dss_1.Tables["INV"].Rows[0][0].ToString();
                    string FSUPPLIERName = FULLNAME.Substring(FULLNAME.IndexOf("\n")+1, FULLNAME.Length-FULLNAME.IndexOf("\n")-1); //供应商名称
                    long FSUPPLIERID = 0;
                    DataRow[] dr_ = dt_Supplier.Select("FName='" + FSUPPLIERName + "' and FUSEORGID=" + this.Context.CurrentOrganizationInfo.ID);
                    if (dr_.Length > 0)
                    {
                        FSUPPLIERID = Convert.ToInt64(dr_[0]["FSUPPLIERID"]);
                    }

                    if (FSUPPLIERID == 0)
                    {
                        this.View.ShowMessage("未找到名称为“" + FSUPPLIERName + "”的供应商，请检查！", MessageBoxType.Advise);
                        this._FileList.Clear();
                        this.FileNameList.Clear();
                        this.View.GetControl("F_JD_FileUpdate").SetValue(DBNull.Value);
                        this.View.Refresh();
                        return;
                    }
                     
                    string InvoiceNo = dt_INV.Rows[1][5].ToString().Trim();

                    string FDate = DateTime.FromOADate(Convert.ToInt32(dt_INV.Rows[0][5].ToString().Trim())).ToString("d");
                    FDate = DateTime.Parse(FDate).ToString("yyyy-MM-dd");
                     
                    string sql_Inv = @"select 1 from t_STK_InStock where F_PAEZ_Text='" + InvoiceNo + "'";

                    DataSet ds_Inv = DBServiceHelper.ExecuteDataSet(this.Context, sql_Inv);
                    if (ds_Inv.Tables[0].Rows.Count > 0)
                    {
                        this.View.ShowMessage("Invoice No：“" + InvoiceNo + "”系统已存在，请检查！", MessageBoxType.Advise);
                        this._FileList.Clear();
                        this.FileNameList.Clear();
                        this.View.GetControl("F_JD_FileUpdate").SetValue(DBNull.Value);
                        this.View.Refresh();
                        return;
                    }
                     
                    string sql_Ex = System.Environment.NewLine + @"SELECT t0.FREVERSEEXRATE as FEXCHANGERATE FROM T_BD_Rate t0 WHERE t0.FFORBIDSTATUS = 'A' and FCYTOID =7 and FCYFORID=1 and GETDATE()>=FBEGDATE and GETDATE()<=FENDDATE and FUSEORGID=" + this.Context.CurrentOrganizationInfo.ID + "" + Environment.NewLine + @"
                    union 
                    SELECT t0.FEXCHANGERATE FROM T_BD_Rate t0 WHERE t0.FFORBIDSTATUS = 'A' and FCYTOID =1 and FCYFORID=7 and GETDATE()>=FBEGDATE and GETDATE()<=FENDDATE and FUSEORGID=" + this.Context.CurrentOrganizationInfo.ID + "" + Environment.NewLine + @"
                    OPTION ( MAXDOP 0)";

                    DataSet ds_Ex = DBServiceHelper.ExecuteDataSet(this.Context, sql_Ex);

                    //if (ds_Ex.Tables[0].Rows.Count < 1)
                    //{
                    //    this.View.ShowMessage("当前有效时间内未设置USD->RMB的汇率体系，请检查！", MessageBoxType.Advise);
                    //    this._FileList.Clear();
                    //    this.FileNameList.Clear();
                    //    this.View.GetControl("F_JD_FileUpdate").SetValue(DBNull.Value);
                    //    this.View.Refresh();
                    //    return;
                    //}
                     //汇率暂时注释
                    //decimal FEXCHANGERATE = Convert.ToDecimal(ds_Ex.Tables[0].Rows[0]["FEXCHANGERATE"]);
                    decimal FEXCHANGERATE = 1;

                    #region 查询选单数据
                    string sql_3 = System.Environment.NewLine + @"SELECT t0.FBILLNO fbillno,
                    t0.fdate,
                    t0.FSUPPLIERID,
                    t0.fdocumentstatus,
                    t0.fpurchaseorgid ,
                    t0.fclosestatus,
                    t3.fmaterialid,
                    t3.funitid,
                    t3.FQTY- t3_R.FBASEJOINQTY as fqty ,
                    t3_D.fdeliverydate ,
                    t3.fgiveaway ,
                    t3.fmrpclosestatus ,
                    t3.fbflowid ,
                    t0.fbilltypeid ,
                    t0.fpurchaseorgid ,
                    t0.fobjecttypeid ,
                    t0.FID fid,
                    t3.FENTRYID t3_fentryid,
                    t3.FSeq t3_fseq,
					t9.FEXCHANGERATE,t9.FSETTLECURRID,t9.FLOCALCURRID
                    FROM t_PUR_POOrder t0 
                    LEFT OUTER JOIN t_PUR_POOrderEntry t3 ON t0.FID = t3.FID 
                    LEFT OUTER JOIN t_PUR_POOrderEntry_F t3_F ON t3.FENTRYID = t3_F.FENTRYID 
                    LEFT OUTER JOIN t_PUR_POOrderEntry_D t3_D ON t3.FENTRYID = t3_D.FENTRYID 
                    LEFT OUTER JOIN t_PUR_POOrderEntry_R t3_R ON t3.FENTRYID = t3_R.FENTRYID 
                    LEFT OUTER JOIN T_BD_MATERIAL st31 ON t3.FMATERIALID = st31.FMATERIALID 
                    LEFT OUTER JOIN T_BD_MATERIAL st31_O ON (st31.FMasterId = st31_O.FMasterId AND (st31_O.FUseOrgId = 0 OR st31_O.FUseOrgId = " + this.Context.CurrentOrganizationInfo.ID + ")) " + System.Environment.NewLine + @"
                    LEFT OUTER JOIN t_BD_MaterialBase st317_O ON st31_O.FMATERIALID = st317_O.FMATERIALID 
                    LEFT OUTER JOIN T_BD_MATERIALQUALITY st321_O ON st31_O.FMATERIALID = st321_O.FMATERIALID
                    left OUTER join T_PUR_POORDERFIN t9 on t0.FID=t9.FID
                    WHERE ((((((((((t0.FPURCHASEORGID = " + this.Context.CurrentOrganizationInfo.ID + "" + Environment.NewLine + @"
                    AND (t3_D.FRECEIVEORGID = " + this.Context.CurrentOrganizationInfo.ID + "" + Environment.NewLine + @"
                    OR t3_D.FRECEIVEORGID = 0
                    OR (t3_D.FRECEIVEORGID IS NULL)))
                    AND t3_F.FSETTLEORGID = " + this.Context.CurrentOrganizationInfo.ID + ")" + Environment.NewLine + @"
                    AND t3_D.FREQUIREORGID = " + this.Context.CurrentOrganizationInfo.ID + ")" + Environment.NewLine + @"
                    AND (t3_D.FRECEIVEORGID = " + this.Context.CurrentOrganizationInfo.ID + "" + Environment.NewLine + @"
                    OR (t3_D.FRECEIVEORGID = 0
                    AND t3_D.FREQUIREORGID IN (0, " + this.Context.CurrentOrganizationInfo.ID + "))))" + Environment.NewLine + @"
                    AND (t3.FBFLOWID = '' OR t3.FBFLOWID = ' ' 
                    OR t3.FBFLOWID = '182c38f9-e371-455e-9672-fad2b11b61e4'
                    OR t3.FBFLOWID = '6af8ef8b-5bb8-4cdc-9972-2f71364b45d8'
                    OR t3.FBFLOWID = '7127460d-b38e-4a2c-b783-72d5f0ac85b3'
                    OR t3.FBFLOWID = 'a6d79725-e25a-482a-a449-a867367c2b97'
                    OR t3.FBFLOWID = 'b27cec31-a12b-4530-8696-885e8f016280'))
                    AND ((((((((t0.FDOCUMENTSTATUS = 'C'
                    AND t0.FCANCELSTATUS = 'A')
                    AND t0.FCLOSESTATUS = 'A')
                    AND t3.FMRPFREEZESTATUS = 'A')
                    AND t3.FMRPTERMINATESTATUS = 'A')
                    AND t3.FMRPCLOSESTATUS = 'A')
                    AND (t3.FCHANGEFLAG <> N'I'))
                    AND (t3_D.FBASEDELIVERYMAXQTY > t3_R.FBASESTOCKINQTY))
                    AND (t3_D.FBASEDELIVERYMAXQTY > t3_R.FBASEJOINQTY))
                    AND (st317_O.FISINVENTORY = '1'
                    AND ((st31_O.FUSEORGID = " + this.Context.CurrentOrganizationInfo.ID + "" + Environment.NewLine + @"
                    AND t3_D.FRECEIVEORGID = 0)
                    OR st31_O.FUSEORGID = t3_D.FRECEIVEORGID)))
                    AND (st321_O.FCHECKINCOMING = '0'
                    AND ((st31_O.FUSEORGID = " + this.Context.CurrentOrganizationInfo.ID + "" + Environment.NewLine + @"
                    AND t3_D.FRECEIVEORGID = 0)
                    OR st31_O.FUSEORGID = t3_D.FRECEIVEORGID)))
                    AND t0.FBILLTYPEID IN ('6d01d059713d42a28bb976c90a121142'))
                    AND t0.FPURCHASEORGID IN (" + this.Context.CurrentOrganizationInfo.ID + ")) and t0.FSUPPLIERID=" + FSUPPLIERID + " --供应商id" + Environment.NewLine + @"
                    AND t0.FOBJECTTYPEID = 'PUR_PurchaseOrder' and t9.FSETTLECURRID=7) 
                    order by fdate
                    OPTION ( MAXDOP 0)";
                    #endregion

                    DataSet ds_3 = DBServiceHelper.ExecuteDataSet(this.Context, sql_3);
                    DataTable dt_src = ds_3.Tables[0];
                     

                    DataTable dt = GetAnalysisTxt(dt_PL, dt_Price);
                    string err_row = "";
                    
                    #region 源单规则 可能需要拆分datatable  进行拆板
                    DataTable dt_ = dt.Clone(); int m = 0;
                    int FUnitID = 0; int FItemID = 0; string AuxNumber = "";
                    string oldFPackedNo = "";
                    int FCartonNo = 1;
                    foreach (DataRow dr in dt.Rows)
                    {
                        FUnitID = 0; FItemID = 0; m++;
                        DataRow dr_item = dt_Item.Rows.Find(dr["FPartID"].ToString());
                        if (dr_item == null)
                        {
                            err_row += "第【" + m + "】行分录，物料代码【" + dr["FPartID"].ToString() + "】不存在或未提交审核。\r\n";
                            continue;
                        }
                        else
                        {
                            AuxNumber = dr["FFuZhu"].ToString();
                            if (dt_Aux.Rows.Find(AuxNumber) == null)
                            {
                                err_row += "第【" + m + "】行分录，辅助属性代码“" + AuxNumber + "”不存在或未提交审核。\r\n";
                                continue;
                            }

                            FUnitID = Convert.ToInt32(dr_item["FBASEUNITID"]);
                            FItemID = Convert.ToInt32(dr_item["FMASTERID"]);
                            DataRow[] dr_Src = dt_src.Select("fmaterialid=" + FItemID + " and funitid=" + FUnitID);
                            if (dr_Src.Length < 1)
                            {
                                
                                dr["FSrcBillNo"] = "";
                                dr["FMustQty"] = 0;
                                //获取板号
                                string FPackedNo = dr["FPackedNo"].ToString();
                                if (FPackedNo != oldFPackedNo)
                                {
                                    FCartonNo = 1;
                                }
                                //根据数量计算箱数 根据具体箱数生成行数
                                int FXiangQty = Convert.ToInt16(dr["FXiangQty"].ToString());
                                double FQTY = Convert.ToDouble(dr["FQty"]);
                                //int TotalFCartonNo = FQTY % FXiangQty;
                                for (int i=1;i<= FXiangQty; i++)
                                {
                                    //修改箱号
                                    dr["FCartonNo"] = FCartonNo;
                                    dr["FUnit"] = FUnitID;
                                    //应发数量
                                    dr["FQty"] = FQTY / FXiangQty;
                                    dr["_FQty"] = FQTY / FXiangQty;
                                    dt_.Rows.Add(dr.ItemArray);
                                    oldFPackedNo = FPackedNo;
                                    FCartonNo++;
                                }
                                continue;
                            }
                            else
                            {
                                decimal FQty = Convert.ToDecimal(dr["FQty"]);
                                decimal SumQty = Convert.ToDecimal(dr_Src.CopyToDataTable().Compute("Sum(fqty)", ""));
                                if (FQty > SumQty)
                                {
                                    err_row += "第【" + m + "】行，物料【" + dr["FPartID"].ToString() + "（" + FUnitID + "）】源单采购订单数量不足。\r\n";
                                    continue;
                                }

                                decimal fqty_s = 0;
                                foreach (DataRow dr_s in dr_Src)
                                {
                                    fqty_s += Convert.ToDecimal(dr_s["fqty"]);
                                    if (fqty_s == 0)
                                        continue;
                                    if (FQty <= fqty_s)
                                    {
                                        dr["FSrcBillNo"] = dr_s["fbillno"];
                                        dr["FSrcEntryID"] = dr_s["t3_fentryid"];
                                        dr_s["fqty"] = fqty_s - FQty;
                                        dr["FQty"] = FQty;
                                        dr["FMustQty"] = fqty_s;
                                        dr["FNW"] = Convert.ToDecimal(dr["_FQty"]) == 0 ? 0 : Math.Round(Convert.ToDecimal(dr["_FNW"]) * FQty / Convert.ToDecimal(dr["_FQty"]), 2, MidpointRounding.AwayFromZero);
                                        dr["FGW"] = Convert.ToDecimal(dr["_FQty"]) == 0 ? 0 : Math.Round(Convert.ToDecimal(dr["_FGW"]) * FQty / Convert.ToDecimal(dr["_FQty"]), 2, MidpointRounding.AwayFromZero);
                                        dt_.Rows.Add(dr.ItemArray);
                                        break;
                                    }
                                    else
                                    {
                                        //拆分行
                                        DataRow dr_n = dt.NewRow();
                                        dr_n.ItemArray = dr.ItemArray;
                                        dr_n["FQty"] = Convert.ToDecimal(dr_s["fqty"]);
                                        FQty = FQty - Convert.ToDecimal(dr_s["fqty"]);
                                        dr_n["FSrcBillNo"] = dr_s["fbillno"];
                                        dr_n["FSrcEntryID"] = dr_s["t3_fentryid"];
                                        dr_n["FMustQty"] = dr_s["fqty"];
                                        dr_n["FNW"] = Convert.ToDecimal(dr["_FQty"]) == 0 ? 0 : Math.Round(Convert.ToDecimal(dr_n["_FNW"]) * Convert.ToDecimal(dr_s["fqty"]) / Convert.ToDecimal(dr["_FQty"]), 2, MidpointRounding.AwayFromZero);
                                        dr_n["FGW"] = Convert.ToDecimal(dr["_FQty"]) == 0 ? 0 : Math.Round(Convert.ToDecimal(dr_n["_FGW"]) * Convert.ToDecimal(dr_s["fqty"]) / Convert.ToDecimal(dr["_FQty"]), 2, MidpointRounding.AwayFromZero);
                                        dt_.Rows.Add(dr_n.ItemArray);
                                        dr_s["fqty"] = 0;
                                    }
                                }
                            }
                        }
                    }
                    if (err_row != "")
                    {
                        this.View.ShowMessage(err_row, MessageBoxType.Advise);
                        this._FileList.Clear();
                        this.FileNameList.Clear();
                        this.View.GetControl("F_JD_FileUpdate").SetValue(DBNull.Value);
                        this.View.Refresh();
                        return;
                    }

                    #endregion

                    try
                    {
                   
                        if (dt.Rows.Count > 0)
                        {


                            #region 新增单据
                            //string result = "";
                            //IMetaDataService mService = Kingdee.BOS.App.ServiceHelper.GetService<IMetaDataService>();
                            //FormMetadata meta = mService.Load(this.Context, "STK_InStock") as FormMetadata;

                            //BusinessInfo info = meta.BusinessInfo;
                            //IResourceServiceProvider formServiceProvider = meta.BusinessInfo.GetForm().GetFormServiceProvider(true);
                            //IBillViewService billViewService = formServiceProvider.GetService(typeof(IDynamicFormView)) as IBillViewService;

                            //IBillView bill_view = (IBillView)billViewService;

                            //DynamicObject leavebill = new DynamicObject(meta.BusinessInfo.GetDynamicObjectType());
                            ////表头
                            //string BillTypevalue = "a1ff32276cd9469dad3bf2494366fa4f";
                            //BaseDataField BillType = meta.BusinessInfo.GetField("FBillTypeID") as BaseDataField;
                            //IViewService viewService = Kingdee.BOS.App.ServiceHelper.GetService<IViewService>();
                            //DynamicObject[] djlx = viewService.LoadFromCache(this.Context, new object[] { BillTypevalue }, BillType.RefFormDynamicObjectType);
                            //BillType.RefIDDynamicProperty.SetValue(leavebill, BillTypevalue);
                            //BillType.DynamicProperty.SetValue(leavebill, djlx[0]);
                            ////业务类型BusinessType
                            //leavebill["BusinessType"] = "CG";
                            ////作废状态
                            //leavebill["CancelStatus"] = "A";
                            ////库存组织
                            //BaseDataField FSTOCKORGID = meta.BusinessInfo.GetField("FSTOCKORGID") as BaseDataField;
                            //leavebill["StockOrgId_Id"] = Convert.ToString(this.Context.CurrentOrganizationInfo.ID);
                            //leavebill["StockOrgId"] = viewService.LoadSingle(this.Context, this.Context.CurrentOrganizationInfo.ID, FSTOCKORGID.RefFormDynamicObjectType);
                            ////需求组织
                            //BaseDataField FDEMANDORGID = meta.BusinessInfo.GetField("FDEMANDORGID") as BaseDataField;
                            //leavebill["DemandOrgId_Id"] = Convert.ToString(this.Context.CurrentOrganizationInfo.ID);
                            //leavebill["DemandOrgId"] = viewService.LoadSingle(this.Context, this.Context.CurrentOrganizationInfo.ID, FDEMANDORGID.RefFormDynamicObjectType);

                            ////创建日期
                            //leavebill["CreateDate"] = DateTime.Now;
                            ////日期
                            //leavebill["Date"] = FDate;
                            //leavebill["OwnerTypeIdHead"] = "BD_OwnerOrg";
                            ////货主
                            //BaseDataField FOWNERID = meta.BusinessInfo.GetField("FOWNERID") as BaseDataField;
                            //leavebill["OwnerIdHead_Id"] = Convert.ToString(this.Context.CurrentOrganizationInfo.ID);
                            //leavebill["OwnerIdHead"] = viewService.LoadSingle(this.Context, this.Context.CurrentOrganizationInfo.ID, FOWNERID.RefFormDynamicObjectType);
                            ////采购组织

                            //BaseDataField FPURCHASEORGID = meta.BusinessInfo.GetField("FPURCHASEORGID") as BaseDataField;
                            //leavebill["PurchaseOrgId_Id"] = Convert.ToString(this.Context.CurrentOrganizationInfo.ID);
                            //leavebill["PurchaseOrgId"] = viewService.LoadSingle(this.Context, this.Context.CurrentOrganizationInfo.ID, FPURCHASEORGID.RefFormDynamicObjectType);
                            ////供应商
                            //BaseDataField FSUPPLIERID2 = meta.BusinessInfo.GetField("FSUPPLIERID") as BaseDataField;
                            //leavebill["SupplierId_Id"] = Convert.ToString(FSUPPLIERID);
                            //leavebill["SupplierId"] = viewService.LoadSingle(this.Context, FSUPPLIERID, FSUPPLIERID2.RefFormDynamicObjectType);
                            ////供货方
                            //BaseDataField FSUPPLYID = meta.BusinessInfo.GetField("FSUPPLYID") as BaseDataField;
                            //leavebill["SupplyId_Id"] = Convert.ToString(FSUPPLIERID);
                            //leavebill["SupplyId"] = viewService.LoadSingle(this.Context, FSUPPLIERID, FSUPPLYID.RefFormDynamicObjectType);
                            ////结算方
                            //BaseDataField FSETTLEID = meta.BusinessInfo.GetField("FSETTLEID") as BaseDataField;
                            //leavebill["SettleId_Id"] = Convert.ToString(FSUPPLIERID);
                            //leavebill["SettleId"] = viewService.LoadSingle(this.Context, FSUPPLIERID, FSETTLEID.RefFormDynamicObjectType);

                            ////收款方
                            //BaseDataField FCHARGEID = meta.BusinessInfo.GetField("FCHARGEID") as BaseDataField;
                            //leavebill["ChargeId_Id"] = Convert.ToString(FSUPPLIERID);
                            //leavebill["ChargeId"] = viewService.LoadSingle(this.Context, FSUPPLIERID, FCHARGEID.RefFormDynamicObjectType);

                            ////Invoice No
                            //leavebill["F_PAEZ_Text"] = InvoiceNo;

                            ////财务信息InStockFin
                            //DynamicObjectCollection EntityRows = leavebill["InStockFin"] as DynamicObjectCollection;
                            //Entity InStockFin = meta.BusinessInfo.GetEntity("FInStockFin"); //分录
                            //DynamicObject EntityRow = new DynamicObject(InStockFin.DynamicObjectType);

                            ////结算组织
                            //BaseDataField FSETTLEORGID = meta.BusinessInfo.GetField("FSETTLEORGID") as BaseDataField;
                            //EntityRow["SettleOrgId_Id"] = Convert.ToString(this.Context.CurrentOrganizationInfo.ID);
                            //EntityRow["SettleOrgId"] = viewService.LoadSingle(this.Context, this.Context.CurrentOrganizationInfo.ID, FSETTLEORGID.RefFormDynamicObjectType);
                            ////结算币别SettleCurrId
                            //BaseDataField FSETTLECURRID = meta.BusinessInfo.GetField("FSETTLECURRID") as BaseDataField;
                            //EntityRow["SettleCurrId_Id"] = Convert.ToString(7);
                            //EntityRow["SettleCurrId"] = viewService.LoadSingle(this.Context, 7, FSETTLECURRID.RefFormDynamicObjectType);
                            ////汇率
                            //EntityRow["ExchangeRate"] = FEXCHANGERATE;
                            ////本位币
                            //BaseDataField FLOCALCURRID = meta.BusinessInfo.GetField("FLOCALCURRID") as BaseDataField;
                            //EntityRow["LocalCurrId_Id"] = 1;
                            //EntityRow["LocalCurrId"] = viewService.LoadSingle(this.Context, 1, FLOCALCURRID.RefFormDynamicObjectType);
                            ////汇率类型
                            //BaseDataField FEXCHANGETYPEID = meta.BusinessInfo.GetField("FEXCHANGETYPEID") as BaseDataField;
                            //EntityRow["ExchangeTypeId_Id"] = 1;
                            //EntityRow["ExchangeTypeId"] = viewService.LoadSingle(this.Context, 1, FEXCHANGETYPEID.RefFormDynamicObjectType);
                            ////定价时点
                            //EntityRow["PriceTimePoint"] = 1;
                            //EntityRows.Add(EntityRow);

                            ////表体数据包
                            //DynamicObjectCollection leaveEntityRows = leavebill["InStockEntry"] as DynamicObjectCollection;
                            //Entity cbtjdEntry = meta.BusinessInfo.GetEntity("FInStockEntry"); //分录
                            //sql_3 = "select  max(FENTRYID) from  T_STK_INSTOCKENTRY";
                            //long maxFID = DBServiceHelper.ExecuteScalar<Int64>(this.Context, sql_3, 0, null);
                            //for (int i = 0; i < dt_.Rows.Count; i++)
                            //{

                            //    DataRow dr_item = dt_Item.Rows.Find(dt_.Rows[i]["FPartID"].ToString());
                            //    //新增一行Convert.ToInt32(dr_item["FMASTERID"])
                            //    DynamicObject newLeaveEntityRow = new DynamicObject(cbtjdEntry.DynamicObjectType);
                            //    //newLeaveEntityRow
                            //    newLeaveEntityRow["Seq"] = i + 1;
                            //    //newLeaveEntityRow["Id"] = maxFID+i+1;
                            //    //物料
                            //    BaseDataField FMATERIALID = meta.BusinessInfo.GetField("FMATERIALID") as BaseDataField;
                            //    newLeaveEntityRow["MaterialId_Id"] = Convert.ToInt32(dr_item["FMASTERID"]);
                            //    newLeaveEntityRow["MaterialId"] = viewService.LoadSingle(this.Context, Convert.ToInt32(dr_item["FMASTERID"]), FMATERIALID.RefFormDynamicObjectType);
                            //    //辅助属性
                            //   // BaseDataField FAUXPROPID = meta.BusinessInfo.GetField("FAUXPROPID") as BaseDataField;
                            //    newLeaveEntityRow["AuxPropId_Id"] = 100002;
                            //   // newLeaveEntityRow["AuxPropId"] = viewService.LoadSingle(this.Context, 100002, FAUXPROPID.RefFormDynamicObjectType);

                            //    //采购单位

                            //    BaseDataField FREMAININSTOCKUNITID = meta.BusinessInfo.GetField("FREMAININSTOCKUNITID") as BaseDataField;
                            //    newLeaveEntityRow["RemainInStockUnitId_Id"] = Convert.ToString(dt_.Rows[i]["FUnit"].ToString());
                            //    newLeaveEntityRow["RemainInStockUnitId"] = viewService.LoadSingle(this.Context, dt_.Rows[i]["FUnit"].ToString(), FREMAININSTOCKUNITID.RefFormDynamicObjectType);

                            //    //基本单位
                            //    BaseDataField FBASEUNITID = meta.BusinessInfo.GetField("FBASEUNITID") as BaseDataField;
                            //    newLeaveEntityRow["BaseUnitID_Id"] = Convert.ToString(dt_.Rows[i]["FUnit"].ToString());
                            //    newLeaveEntityRow["BaseUnitID"] = viewService.LoadSingle(this.Context, dt_.Rows[i]["FUnit"].ToString(), FBASEUNITID.RefFormDynamicObjectType);
                            //    //计价单位
                            //    BaseDataField FPRICEUNITID = meta.BusinessInfo.GetField("FPRICEUNITID") as BaseDataField;
                            //    newLeaveEntityRow["PriceUnitID_Id"] = Convert.ToString(dt_.Rows[i]["FUnit"].ToString());
                            //    newLeaveEntityRow["PriceUnitID"] = viewService.LoadSingle(this.Context, dt_.Rows[i]["FUnit"].ToString(), FPRICEUNITID.RefFormDynamicObjectType);
                            //    //辅单位
                            //    BaseDataField FEXTAUXUNITID = meta.BusinessInfo.GetField("FEXTAUXUNITID") as BaseDataField;
                            //    newLeaveEntityRow["ExtAuxUnitId_Id"] = Convert.ToString(dt_.Rows[i]["FUnit"].ToString());
                            //    newLeaveEntityRow["ExtAuxUnitId"] = viewService.LoadSingle(this.Context, dt_.Rows[i]["FUnit"].ToString(), FEXTAUXUNITID.RefFormDynamicObjectType);
                            //    //库存状态
                            //    BaseDataField FSTOCKSTATUSID = meta.BusinessInfo.GetField("FSTOCKSTATUSID") as BaseDataField;
                            //    newLeaveEntityRow["StockStatusId_Id"] = 10000;
                            //    newLeaveEntityRow["StockStatusId"] = viewService.LoadSingle(this.Context, 10000, FSTOCKSTATUSID.RefFormDynamicObjectType);
                            //    //仓库
                            //    BaseDataField FSTOCKID = meta.BusinessInfo.GetField("FSTOCKID") as BaseDataField;
                            //    newLeaveEntityRow["StockId_Id"] = 123066;
                            //    newLeaveEntityRow["StockId"] = viewService.LoadSingle(this.Context, 123066, FSTOCKID.RefFormDynamicObjectType);
                            //    //税率(%)
                            //    newLeaveEntityRow["TaxRate"] = 16;
                            //    //含税单价
                            //    newLeaveEntityRow["TaxPrice"] = 10;
                            //    //库存单位
                            //    BaseDataField FUNITID = meta.BusinessInfo.GetField("FUNITID") as BaseDataField;
                            //    newLeaveEntityRow["UnitID_Id"] = Convert.ToString(dt_.Rows[i]["FUnit"].ToString());
                            //    newLeaveEntityRow["UnitID"] = viewService.LoadSingle(this.Context, dt_.Rows[i]["FUnit"].ToString(), FUNITID.RefFormDynamicObjectType);
                            //    //货主类型
                            //    newLeaveEntityRow["OWNERTYPEID"] = "BD_OwnerOrg";
                            //    //货主
                            //    BaseDataField FOWNERID2 = meta.BusinessInfo.GetField("FOWNERID") as BaseDataField;
                            //    newLeaveEntityRow["OWNERID_Id"] = Convert.ToString(this.Context.CurrentOrganizationInfo.ID);
                            //    newLeaveEntityRow["OWNERID"] = viewService.LoadSingle(this.Context, this.Context.CurrentOrganizationInfo.ID, FOWNERID2.RefFormDynamicObjectType);
                            //    //保管者类型
                            //    newLeaveEntityRow["KeeperTypeID"] = "BD_OwnerOrg";
                            //    //保管者类型
                            //    BaseDataField FKEEPERID = meta.BusinessInfo.GetField("FKEEPERID") as BaseDataField;
                            //    newLeaveEntityRow["KeeperID_Id"] = Convert.ToString(this.Context.CurrentOrganizationInfo.ID);
                            //    newLeaveEntityRow["KeeperID"] = viewService.LoadSingle(this.Context, this.Context.CurrentOrganizationInfo.ID, FKEEPERID.RefFormDynamicObjectType);
                            //    //产品类型
                            //    newLeaveEntityRow["RowType"] = "Standard";
                            //    //应付关闭状态
                            //    newLeaveEntityRow["PayableCloseStatus"] = "A";
                            //    //采购数量
                            //    newLeaveEntityRow["RemainInStockQty"] = Convert.ToDecimal(dt_.Rows[i]["FQty"]);
                            //    //采购基本数量
                            //    newLeaveEntityRow["RemainInStockBaseQty"] = Convert.ToDecimal(dt_.Rows[i]["FQty"]);
                            //    //Lot
                            //    // newLeaveEntityRow["Lot_Id"] = 205556;
                            //    // newLeaveEntityRow["Lot"] = 205556;
                            //    newLeaveEntityRow["Lot_Text"] = InvoiceNo + "_" + dt_.Rows[i]["FPackedNo"].ToString() + "_" + dt_.Rows[i]["FCartonNo"].ToString();
                            //    //实收数量
                            //    newLeaveEntityRow["RealQty"] = Convert.ToDecimal(dt_.Rows[i]["FQty"]);
                            //    //实收数量(辅单位)
                            //    newLeaveEntityRow["ExtAuxUnitQty"] = Convert.ToDecimal(dt_.Rows[i]["FQty"]);
                            //    //计价数量
                            //    newLeaveEntityRow["PriceUnitQty"] = Convert.ToDecimal(dt_.Rows[i]["FQty"]);
                            //    // 应收数量
                            //    newLeaveEntityRow["MustQty"] = Convert.ToDecimal(dt_.Rows[i]["FQty"]);
                            //    ////配货数量(基本单位)
                            //    //newLeaveEntityRow["FAllotBaseQty"] = Convert.ToDecimal(dt_.Rows[i]["FQty"]);
                            //    ////关联数量(基本单位)
                            //    //newLeaveEntityRow["BaseJoinQty"] = Convert.ToDecimal(dt_.Rows[i]["FQty"]);
                            //    ////消耗汇总基本单位数量
                            //    //newLeaveEntityRow["BaseConsumeSumQty"] = Convert.ToDecimal(dt_.Rows[i]["FQty"]);
                            //    //库存基本数量
                            //    newLeaveEntityRow["BaseUnitQty"] = Convert.ToDecimal(dt_.Rows[i]["FQty"]);
                            //    newLeaveEntityRow["F_PAEZ_Text1"] = dt_.Rows[i]["FPackedNo"].ToString();
                            //    newLeaveEntityRow["F_PAEZ_Text2"] = dt_.Rows[i]["FCartonNo"].ToString();
                            //    newLeaveEntityRow["F_PAEZ_Decimal"] = Convert.ToDecimal(dt_.Rows[i]["_FQty"]);
                            //    newLeaveEntityRow["F_PAEZ_Decimal1"] = Convert.ToDecimal(dt_.Rows[i]["FNW"]);
                            //    newLeaveEntityRow["F_PAEZ_Decimal2"] = Convert.ToDecimal(dt_.Rows[i]["FGW"]);
                            //    newLeaveEntityRow["F_PAEZ_Decimal3"] = Convert.ToDecimal(dt_.Rows[i]["_FNW"]);
                            //    newLeaveEntityRow["F_PAEZ_Decimal4"] = Convert.ToDecimal(dt_.Rows[i]["_FGW"]);
                            //    //卡板尺寸
                            //    newLeaveEntityRow["F_PAEZ_Text3"] = dt_.Rows[i]["FKaBanSize"].ToString();
                            //    //外箱尺寸
                            //    newLeaveEntityRow["F_PAEZ_Text4"] = dt_.Rows[i]["FWaiXiangSize"].ToString();
                            //    //品牌
                            //    newLeaveEntityRow["F_PAEZ_Text5"] = dt_.Rows[i]["FPinPai"].ToString();
                            //    leaveEntityRows.Add(newLeaveEntityRow);
                            //}

                            ////cbtjdlist.Add(leavebill);
                            ////保存
                            //IOperationResult SaveResult = Operation2(leavebill, meta);
                            //string msg = "";
                            //if (!SaveResult.IsSuccess)
                            //{
                            //    foreach (var item in SaveResult.ValidationErrors)
                            //    {
                            //        result += result + item.Message;
                            //    }
                            //    if (!SaveResult.InteractionContext.IsNullOrEmpty())
                            //    {
                            //        result += result + SaveResult.InteractionContext.SimpleMessage;
                            //    }
                            //}
                            //else
                            //{
                            //    foreach (var item in SaveResult.OperateResult)
                            //    {
                            //        result += result + item.Message;
                            //        string fnmber = item.Number;
                            //        sql_3 = "update t_STK_InStock set  FOBJECTTYPEID='STK_InStock'  where  FBILLNO='" + fnmber + "'";
                            //        DBServiceHelper.Execute(this.Context, sql_3);

                            //    }
                            //}
                            // Operation(cbtjd, cbtjdlist);

                            #endregion

                            #region 方法二： 创建视图、模型，模拟手工新增，会触发大部分的表单服务和插件
                            FormMetadata meta = MetaDataServiceHelper.Load(this.Context, "STK_InStock") as FormMetadata;
                            BusinessInfo info = meta.BusinessInfo;
                            IResourceServiceProvider formServiceProvider = meta.BusinessInfo.GetForm().GetFormServiceProvider(true);
                            IBillViewService billViewService = formServiceProvider.GetService(typeof(IDynamicFormView)) as IBillViewService;

                            /******创建单据打开参数*************/
                            Form form = meta.BusinessInfo.GetForm();
                            BillOpenParameter billOpenParameter = new BillOpenParameter(form.Id, meta.GetLayoutInfo().Id);
                            billOpenParameter = new BillOpenParameter(form.Id, string.Empty);
                            billOpenParameter.Context = this.Context;
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
                            bill_view.Model.SetItemValueByID("FStockOrgId", this.Context.CurrentOrganizationInfo.ID, 0);
                            bill_view.Model.SetValue("FDate", FDate);
                            bill_view.Model.SetItemValueByNumber("FBillTypeID", "RKD03_SYS", 0);
                            bill_view.Model.SetValue("FOwnerTypeIdHead", "BD_OwnerOrg");
                            bill_view.Model.SetItemValueByID("FOwnerIdHead", this.Context.CurrentOrganizationInfo.ID, 0);
                            bill_view.Model.SetItemValueByID("FPurchaseOrgId", this.Context.CurrentOrganizationInfo.ID, 0);
                            bill_view.Model.SetItemValueByID("FSettleOrgId", this.Context.CurrentOrganizationInfo.ID, 0);
                            bill_view.Model.SetItemValueByID("FSupplierId", FSUPPLIERID, 0);
                            bill_view.Model.SetValue("F_PAEZ_Text", InvoiceNo, 0);


                            bill_view.Model.SetItemValueByNumber("FLocalCurrID", "PRE001", 0);
                            bill_view.Model.SetItemValueByNumber("FSettleCurrId", "PRE007", 0);

                            bill_view.Model.SetValue("FExchangeRate", FEXCHANGERATE);
                            bill_view.Model.SetItemValueByNumber("FExchangeTypeID", "HLTX01_SYS", 0);
                            bill_view.Model.SetValue("FPriceTimePoint", 1);

                            for (int i = 0; i < dt_.Rows.Count; i++)
                            {
                                bill_view.Model.CreateNewEntryRow("FInStockEntry");

                                if (dt_.Rows[i]["FSrcBillNo"] != null && dt_.Rows[i]["FSrcBillNo"].ToString() != "")
                                {
                                    bill_view.Model.SetValue("FSRCBILLTYPEID", "PUR_PurchaseOrder", i);
                                    bill_view.Model.SetValue("FSRCBILLNO", dt_.Rows[i]["FSrcBillNo"].ToString(), i);
                                    bill_view.Model.SetValue("FPOORDERENTRYID", Convert.ToInt32(dt_.Rows[i]["FSrcEntryID"]), i);
                                    bill_view.Model.SetValue("FPOORDERNO", dt_.Rows[i]["FSrcBillNo"].ToString(), i);
                                }

                                bill_view.Model.SetItemValueByNumber("FMATERIALID", dt_.Rows[i]["FPartID"].ToString(), i);
                                bill_view.InvokeFieldUpdateService("FMATERIALID", i);
                                bill_view.InvokeFieldUpdateService("FUNITID", i);
                                bill_view.InvokeFieldUpdateService("FBASEUNITID", i);
                                //仓库
                                bill_view.Model.SetItemValueByNumber("FStockId", "CK001", i);

                                bill_view.Model.SetValue("FREALQTY", Convert.ToDecimal(dt_.Rows[i]["FQty"]), i);
                                bill_view.InvokeFieldUpdateService("FREALQTY", i);

                                bill_view.Model.SetValue("FMustQty", Convert.ToDecimal(dt_.Rows[i]["FMustQty"]), i);
                                //板号
                                bill_view.Model.SetValue("F_PAEZ_Text1", dt_.Rows[i]["FPackedNo"].ToString(), i);
                                //箱号
                                bill_view.Model.SetValue("F_PAEZ_Text2", dt_.Rows[i]["FCartonNo"].ToString(), i);
                                //批号
                                // bill_view.Model.SetValue("FLot", InvoiceNo+"_"+dt_.Rows[i]["FPackedNo"].ToString()+"_"+ dt_.Rows[i]["FCartonNo"].ToString(), i);
                                bill_view.Model.SetValue("F_PAEZ_Decimal", Convert.ToDecimal(dt_.Rows[i]["_FQty"]), i);
                                bill_view.Model.SetValue("F_PAEZ_Decimal1", Convert.ToDecimal(dt_.Rows[i]["FNW"]), i);
                                bill_view.Model.SetValue("F_PAEZ_Decimal2", Convert.ToDecimal(dt_.Rows[i]["FGW"]), i);
                                bill_view.Model.SetValue("F_PAEZ_Decimal3", Convert.ToDecimal(dt_.Rows[i]["_FNW"]), i);
                                bill_view.Model.SetValue("F_PAEZ_Decimal4", Convert.ToDecimal(dt_.Rows[i]["_FGW"]), i);

                                bill_view.Model.SetValue("FTaxPrice", Convert.ToDecimal(dt_.Rows[i]["FTaxPrice"]), i);
                                bill_view.InvokeFieldUpdateService("FTaxPrice", i);

                                bill_view.Model.SetItemValueByNumber("$$FAUXPROPID__FF100001", dt_.Rows[i]["FFuZhu"].ToString(), i);

                                bill_view.Model.SetValue("F_PAEZ_TEXT3", dt_.Rows[i]["FKaBanSize"].ToString(), i);
                                bill_view.Model.SetValue("F_PAEZ_TEXT4", dt_.Rows[i]["FWaiXiangSize"].ToString(), i);
                                bill_view.Model.SetValue("F_PAEZ_TEXT5", dt_.Rows[i]["FPinPai"].ToString(), i);
                                bill_view.Model.SetValue("F_PAEZ_INTEGER", dt_.Rows[i]["FXiangQty"], i);

                            }
                            string result = "";
                            if (bill_view.InvokeFormOperation("GenerateLotByCodeRule"))
                            {
                                IOperationResult save_result = bill_view.Model.Save();

                                if (!save_result.IsSuccess)
                                {
                                    foreach (var item in save_result.ValidationErrors)
                                    {
                                        result += result + item.Message;
                                    }
                                    if (!save_result.InteractionContext.IsNullOrEmpty())
                                    {
                                        result += result + save_result.InteractionContext.SimpleMessage;

                                    }
                                    this.View.ShowMessage("引入失败！" + result, MessageBoxType.Advise);
                                }
                                else
                                {
                                    foreach (var item in save_result.OperateResult)
                                    {
                                        result += result + item.Message;
                                        string fnmber = item.Number;
                                        sql_3 = "update t_STK_InStock set  FOBJECTTYPEID='STK_InStock'  where  FBILLNO='" + fnmber + "'";
                                        DBServiceHelper.Execute(this.Context, sql_3);
                                        Import = true;

                                    }
                                    this.View.ShowMessage("引入成功！" + result, MessageBoxType.Notice);
                                }
                                //if (save_result.IsSuccess)
                                //{
                                //    Import = true;
                                //    this.View.ShowMessage("引入成功！", MessageBoxType.Notice);
                                //}
                                //else
                                //{
                                //    for (int mf = 0; mf < save_result.ValidationErrors.Count; mf++)
                                //    {
                                //        result += "\r\n" + save_result.ValidationErrors[mf].Message;
                                //    }
                                //    this.View.ShowMessage("引入失败！" + result, MessageBoxType.Advise);
                                //}
                            }
                            else
                            {
                                this.View.ShowMessage("引入失败，获取批号错误！" + result, MessageBoxType.Advise);
                            }

                            #endregion
                        }

                        }
                    catch (Exception ex)
                    {
                        this.View.ShowMessage("引入失败！详细信息为：\r\n" + ex.Message.ToString(), MessageBoxType.Advise);
                    }

                    this._FileList.Clear();
                    this.FileNameList.Clear();
                    this.View.GetControl("F_JD_FileUpdate").SetValue(DBNull.Value);
                    this.View.Refresh();
                }
            }
            else if (e.Key.EqualsIgnoreCase("F_JD_BTNCancel"))
            {
                this.View.ReturnToParentWindow(new FormResult(Import));
                this.View.Close();
            }
        }
        /// <summary>
        /// 修改保存
        /// </summary>
        /// <param name="newdlm"></param>
        /// <param name="materialmeta"></param>
        /// <returns></returns>
        private IOperationResult Operation2(DynamicObject md, FormMetadata materialmeta)
        {
            //Context ctx = this.Context;
            OperateOption option = OperateOption.Create();
            option.SetIgnoreWarning(true);
            option.SetVariableValue("ignoreTransaction", true);

            //保存
            DynamicObject[] dylist = new DynamicObject[] { md };
            ISaveService saveService = Kingdee.BOS.App.ServiceHelper.GetService<ISaveService>();
            IOperationResult saveresult = saveService.Save(this.Context, materialmeta.BusinessInfo, dylist, option);
            return saveresult;
        }


        public override void BeforeClosed(BeforeClosedEventArgs e)
        {
            base.BeforeClosed(e);
            this.View.ReturnToParentWindow(new FormResult(Import));
        }

        
        private string GetFullFileName(string fileName)
        {
            string dir = "FileUpLoadServices\\UploadFiles";
            return PathUtils.GetPhysicalPath(dir, fileName);
        }

        
        private DataTable GetAnalysisTxt(DataTable DaExSrc,DataTable dt2)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("FPackedNo", typeof(string));
            dt.Columns.Add("FCartonNo", typeof(string));
            dt.Columns.Add("FPartID", typeof(string));
            dt.Columns.Add("FQty", typeof(decimal));
            dt.Columns.Add("_FQty", typeof(decimal));
            dt.Columns.Add("FUnit", typeof(string));
            dt.Columns.Add("FNW", typeof(decimal));
            dt.Columns.Add("FGW", typeof(decimal));
            dt.Columns.Add("_FNW", typeof(decimal));
            dt.Columns.Add("_FGW", typeof(decimal));
            dt.Columns.Add("FSrcBillNo", typeof(string));
            dt.Columns.Add("FSrcEntryID", typeof(int));
            dt.Columns.Add("FMustQty", typeof(decimal));
            dt.Columns.Add("FTaxPrice", typeof(decimal));
            dt.Columns.Add("FXiangQty", typeof(string));
            dt.Columns.Add("FWaiXiangSize", typeof(string));
            dt.Columns.Add("FKaBanSize", typeof(string));
            dt.Columns.Add("FPinPai", typeof(string));
            dt.Columns.Add("FFuZhu", typeof(string));

            DataTable dt_itemPrice = data_itemPrice_Rate(dt2); dt_itemPrice.PrimaryKey = new DataColumn[] { dt_itemPrice.Columns["FNumber"] };

            DataRow dr = dt.NewRow();
            int BiaoShi = 0;
            string FPackedNo = "";
            string Ka = "";
            string PinPai = "";
            for (int jj = 0; jj < DaExSrc.Rows.Count; jj++)
            {
                if (DaExSrc.Rows[jj][0].ToString().Trim() == "以下空白")
                    break;

                if (DaExSrc.Rows[jj][1].ToString().Trim() != "" && DaExSrc.Rows[jj][2].ToString().Trim() != "") //物料行
                {
                    BiaoShi = jj + 1;
                    if (DaExSrc.Rows[jj][0].ToString().Trim() == "")
                    {
                        dr["FPackedNo"] = FPackedNo;
                    }
                    else
                        dr["FPackedNo"] = FPackedNo = DaExSrc.Rows[jj][0].ToString().Trim();

                    dr["FPartID"] = DaExSrc.Rows[jj][1].ToString().Trim();
                    dr["FCartonNo"] = dr["FXiangQty"] = DaExSrc.Rows[jj][4];
                    dr["FQty"] = DaExSrc.Rows[jj][8];
                    dr["FNW"] = DaExSrc.Rows[jj][5];
                    dr["FGW"] = DaExSrc.Rows[jj][6];
                    dr["FWaiXiangSize"] = DaExSrc.Rows[jj][9];

                    if (DaExSrc.Rows[jj][10].ToString().Trim() == "")
                        dr["FKaBanSize"] = Ka;
                    else
                        dr["FKaBanSize"] = Ka = DaExSrc.Rows[jj][10].ToString().Trim();

                    if (DaExSrc.Rows[jj][11].ToString().Trim() == "")
                        dr["FPinPai"] = PinPai;
                    else
                        dr["FPinPai"] = PinPai = DaExSrc.Rows[jj][11].ToString().Trim();

                    dr["FFuZhu"] = DaExSrc.Rows[jj][7];

                    if (dt_itemPrice != null)
                    {
                        if (dt_itemPrice.Rows.Find(DaExSrc.Rows[jj][1].ToString().Trim()) == null)
                            dr["FTaxPrice"] = 0;
                        else
                            dr["FTaxPrice"] = dt_itemPrice.Rows.Find(DaExSrc.Rows[jj][1].ToString().Trim())["FTaxPrice"];
                    }
                    else
                        dr["FTaxPrice"] = 0;
                }
                if (jj == BiaoShi)
                {
                    dr["_FNW"] = DaExSrc.Rows[jj][5].ToString().Trim().Replace("@","");
                    dr["_FGW"] = DaExSrc.Rows[jj][6].ToString().Trim().Replace("@", "");
                    dr["_FQty"] = DaExSrc.Rows[jj][8].ToString().Trim().Replace("@", "");
                    dt.Rows.Add(dr.ItemArray);
                }
            }

            return dt;
        }

        private DataTable data_itemPrice_Rate(DataTable dtR)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("FNumber", typeof(string));
            dt.Columns.Add("FTaxPrice", typeof(string));

            for (int ii = 0; ii < dtR.Rows.Count; ii++)
            {
                if (dtR.Rows[ii][1].ToString().Trim() == "Following blank")
                    break;

                decimal FTaxPrice=0;
                if (!decimal.TryParse(dtR.Rows[ii][5].ToString().Trim(), out FTaxPrice))
                {
                    continue;
                }
                 
                DataRow dr = dt.NewRow();
                dr["FNumber"]=dtR.Rows[ii][1].ToString().Trim();
                dr["FTaxPrice"] = FTaxPrice;
                dt.Rows.Add(dr.ItemArray);
            }

           var query = from g in dt.AsEnumerable()
                       group g by new { t1 = g.Field<string>("FNumber") } into DtGroup
                       select new { 
                           FNumber = DtGroup.Key.t1,
                           FTaxPrice = DtGroup.Max(n => n.Field<string>("FTaxPrice"))
                       };


           DataTable _dt = dt.Clone();
           DataRow _dr = _dt.NewRow();
           if (query.ToList().Count > 0)
           {
               query.ToList().ForEach(q =>
               {
                   _dr["FNumber"] = q.FNumber;
                   _dr["FTaxPrice"] = q.FTaxPrice;
                   _dt.Rows.Add(_dr.ItemArray);
               });
           }
           return _dt;
        }
    }
}
