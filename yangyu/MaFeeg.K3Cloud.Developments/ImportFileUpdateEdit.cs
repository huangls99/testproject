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
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.Metadata.FormElement;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.Metadata.EntityElement;

namespace MaFeeg.K3Cloud.Developments
{
    [Description("PACKING文件导入")]
    [Kingdee.BOS.Util.HotUpdate]
    public class ImportFileUpdateEdit : AbstractDynamicFormPlugIn
    {
         
        private List<string> FileNameList = new List<string> { };
        private List<string> _FileList = new List<string> { };
        private bool Import = false;
        public override void AfterBindData(EventArgs e)
        {
            this.View.GetControl("F_JD_BTNOK").Enabled = false;
            string CustomKey = this.View.OpenParameter.GetCustomParameter("CustomKey").ToString();//获取父级页面传参的参数

            if (CustomKey == "1001")
            {
                LocaleValue str = new LocaleValue("「平板」PACKING引入");
                this.View.SetFormTitle(str);
            }
            else if(CustomKey == "1002")
            {
                LocaleValue str = new LocaleValue("「手机」PACKING引入");
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
                    this.View.ShowMessage("未检测到需要引入的txt文件！", MessageBoxType.Error);
                }
                else
                {
                    string result = "";

                    string _sql = @"select tt.FSUPPLIERID,FName,t1.FUSEORGID from t_BD_Supplier_L tt
                    left join t_BD_Supplier t1 on tt.FSUPPLIERID=t1.FSUPPLIERID
                    where FFORBIDSTATUS='A' and FDOCUMENTSTATUS='C'";

                    _sql += Environment.NewLine +
                    @"select * from [dbo].[T_BAS_SYSTEMPROFILE] where FCATEGORY='STK' and FKEY='STARTSTOCKDATE'";

                    _sql += Environment.NewLine +
                    @"SELECT t2.FSTOCKID,t0.FMASTERID,t0.FNUMBER,FBASEUNITID,t2.FBOXSTANDARDQTY,t0.FDXQTY,t0.FPCSCONVERT FROM T_BD_MATERIAL t0 
                    LEFT OUTER JOIN t_BD_MaterialBase t1 ON t0.FMATERIALID = t1.FMATERIALID 
                    LEFT OUTER JOIN t_BD_MaterialStock t2 ON t0.FMATERIALID = t2.FMATERIALID 
                    LEFT OUTER JOIN T_BD_MATERIAL_L t0_L ON (t0.FMATERIALID = t0_L.FMATERIALID AND t0_L.FLocaleId = 2052) 
                    WHERE t0.FFORBIDSTATUS = 'A' AND t0.FUSEORGID=1 and t0.FDOCUMENTSTATUS ='C' and FUseOrgID=" + this.Context.CurrentOrganizationInfo.ID + " OPTION ( MAXDOP 0)";

                    DataSet ds = DBServiceHelper.ExecuteDataSet(this.Context, _sql);
                    DataTable dt_Supplier = ds.Tables[0];
                    DataTable dt_system = ds.Tables[1]; dt_system.PrimaryKey = new DataColumn[] { dt_system.Columns["FORGID"] };
                    DataTable dt_Item = ds.Tables[2]; dt_Item.PrimaryKey = new DataColumn[] { dt_Item.Columns["FNUMBER"] };

                    for (int f = 0; f < FileNameList.Count; f++)
                    {
                        result += "\r\n《" + _FileList[f] + "》结果:";
                        try
                        {
                            DataRow dr_sys = dt_system.Rows.Find(this.Context.CurrentOrganizationInfo.ID);
                            if (dr_sys == null)
                            {
                                result += "\r\n 当前组织“" + this.Context.CurrentOrganizationInfo.Name + "”未启用库存组织。\r\n________________________________________________________________________\r\n";
                                continue;
                            }

                            string CustomKey = this.View.OpenParameter.GetCustomParameter("CustomKey").ToString();//获取父级页面传参的参数
                            List<object> analysis = new List<object> { };
                            //判断导入txt类型
                            if (CustomKey == "1001") //「平板」PACKING引入
                            {
                                analysis = GetAnalysisTxt(FileNameList[f]);
                            }
                            else if (CustomKey == "1002") //「手机」PACKING引入
                            {
                                analysis = GetAnalysisTxt2(FileNameList[f]);
                            }

                            long FSUPPLIERID = 0;
                            DataRow[] dr_ = dt_Supplier.Select("FName='" + analysis[0].ToString() + "' and FUSEORGID=" + this.Context.CurrentOrganizationInfo.ID);
                            if (dr_.Length > 0)
                            {
                                FSUPPLIERID = Convert.ToInt64(dr_[0]["FSUPPLIERID"]);
                            }

                            if (FSUPPLIERID == 0)
                            {
                                result += "\r\n 未找到名称为“" + analysis[0].ToString() + "”的供应商。\r\n________________________________________________________________________\r\n";
                                continue;
                            }

                            string sql_3 = @"select 1 from t_STK_InStock where F_PAEZ_Text='" + analysis[1].ToString()+"'";
 
                            sql_3 += System.Environment.NewLine + @"SELECT t0.FREVERSEEXRATE as FEXCHANGERATE FROM T_BD_Rate t0 WHERE t0.FFORBIDSTATUS = 'A' and FCYTOID =7 and FCYFORID=1 and GETDATE()>=FBEGDATE and GETDATE()<=FENDDATE and FUSEORGID=" + this.Context.CurrentOrganizationInfo.ID + "" + Environment.NewLine + @"
                            union 
                            SELECT t0.FEXCHANGERATE FROM T_BD_Rate t0 WHERE t0.FFORBIDSTATUS = 'A' and FCYTOID =1 and FCYFORID=7 and GETDATE()>=FBEGDATE and GETDATE()<=FENDDATE and FUSEORGID=" +this.Context.CurrentOrganizationInfo.ID+""+Environment.NewLine+@"
                            OPTION ( MAXDOP 0)";

                            #region 查询选单数据
                            sql_3 += System.Environment.NewLine + @"SELECT t0.FBILLNO fbillno,
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
                            WHERE (((((((((((((t0.FPURCHASEORGID = " + this.Context.CurrentOrganizationInfo.ID + "" + Environment.NewLine + @"
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
                            AND (t0.FBILLTYPEID <> 'b0677860cd16433895be5f520086b69f'))
                            AND (t0.FBILLTYPEID <> 'b8df755fd92b4c2baedef2439c29f793')))
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
                            AND t0.FBILLTYPEID IN ('83d822ca3e374b4ab01e5dd46a0062bd', 'ba3ad5fc48d44271a048da26b615b589', '0023240234df807511e308990e04cf6a'))
                            AND t0.FPURCHASEORGID IN (1, " + this.Context.CurrentOrganizationInfo.ID + ")) and t0.FSUPPLIERID=" + FSUPPLIERID + " --供应商id" + Environment.NewLine + @"
                            AND t0.FOBJECTTYPEID = 'PUR_PurchaseOrder' and t9.FSETTLECURRID=7) 
                            order by fdate
                            OPTION ( MAXDOP 0)";
                            #endregion


                            DataSet ds_3 = DBServiceHelper.ExecuteDataSet(this.Context, sql_3);
                            DataTable dt_head = ds_3.Tables[0];
                            if (dt_head.Rows.Count > 0)
                            {
                                result += "\r\n Invoice No：“" + analysis[1].ToString() + "”系统已存在。\r\n________________________________________________________________________\r\n";
                                continue;
                            }

                            //DataTable dt_ex = ds_3.Tables[1];
                            //if (dt_ex.Rows.Count < 1)
                            //{
                            //    result += "\r\n 当前有效时间内未设置USD->RMB的汇率体系。";
                            //    result += "\r\n________________________________________________________________________\r\n";
                            //    break;
                            //}

                            decimal FEXCHANGERATE =1;

                            DataTable dt_src = ds_3.Tables[2];

                            DataTable dt = (DataTable)analysis[3];
                            string err_row = "";
 
                            DataTable dt_ = dt.Clone(); int m = 0;
                            int FUnitID = 0; int FItemID = 0;
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
                                    //if (CustomKey == "1002")
                                    //{
                                    //    dr["_FQty"] = dr_item["FBOXSTANDARDQTY"];
                                    //    dr["_FNW"] = Convert.ToDecimal(dr["_FQty"]) == 0 ? 0 : Math.Round(Convert.ToDecimal(dr["FNW"]) / (Convert.ToDecimal(dr["FQty"]) / Convert.ToDecimal(dr["_FQty"])), 2, MidpointRounding.AwayFromZero);
                                    //}
 
                                    //dr["_FGW"] = Convert.ToDecimal(dr["_FQty"]) == 0 ? 0 : Math.Round(Convert.ToDecimal(dr["FGW"]) / (Convert.ToDecimal(dr["FQty"]) / Convert.ToDecimal(dr["_FQty"])), 2, MidpointRounding.AwayFromZero);

                                    FUnitID = Convert.ToInt32(dr_item["FBASEUNITID"]);
                                    FItemID = Convert.ToInt32(dr_item["FMASTERID"]);
                                    DataRow[] dr_Src = dt_src.Select("fmaterialid=" + FItemID + " and funitid=" + FUnitID);
                                    if (dr_Src.Length < 1)//判断是否存在源单
                                    {
                                        dr["FSrcBillNo"] = "";
                                        dr["FMustQty"] = 0;
                                        if (CustomKey == "1001") //「平板」PACKING引入
                                        {
                                            #region  平板拆分 
                                            if (dr["FPackedNo"].ToString().Contains("-")) //每一行只有一板号
                                            {
                                                //去掉@
                                                string[] str2 = Regex.Split(dr["FPackedNo"].ToString().Replace("@", "").Trim(), "-", RegexOptions.IgnoreCase);
                                                if (str2[0].ToString() == str2[1].ToString()) //这里一行只有板
                                                {

                                                    if (dr["FCartonNo"].ToString().Contains("-"))
                                                    {
                                                        int newFQty = Convert.ToInt16(dr["FQty"].ToString());
                                                        int new_FQty = Convert.ToInt16(dr["_FQty"].ToString());
                                                        double FNW = Convert.ToDouble(dr["FNW"].ToString());
                                                        double FGW = Convert.ToDouble(dr["FGW"].ToString());
                                                        string[] str = Regex.Split(dr["FCartonNo"].ToString(), "-", RegexOptions.IgnoreCase); //第一种箱号为1-16 
                                                        int FCartonNo = Convert.ToInt16(str[1].ToString()) - Convert.ToInt16(str[0].ToString());
                                                        //进行拆板分箱
                                                        for (int i = Convert.ToInt16(str[0].ToString()); i <= Convert.ToInt16(str[1].ToString()); i++)
                                                        {
                                                            dr["FPackedNo"] = str2[0].ToString();//板号
                                                            dr["FCartonNo"] = i;//箱号
                                                            if (FCartonNo == 0)
                                                            {
                                                                dr["FQty"] = new_FQty;//每一箱数量
                                                                dr["_FNW"] = FNW; //每一箱数量
                                                                dr["_FGW"] = FGW;
                                                                dr["FUnit"] = FUnitID;
                                                                //dr["FMustQty"]= dr["_FQty"]; //每一箱数量
                                                            }
                                                            else
                                                            {
                                                                dr["FUnit"] = FUnitID;
                                                                dr["FQty"] = newFQty / (FCartonNo + 1); //每一箱数量
                                                                dr["_FQty"] = newFQty / (FCartonNo + 1);
                                                                dr["_FNW"] = FNW / (FCartonNo + 1); //每箱净重
                                                                dr["_FGW"] = FGW / (FCartonNo + 1); //每箱毛重
                                                              //dr["FMustQty"] = Convert.ToInt16(dr["FQty"].ToString()) / (FCartonNo + 1);//每一箱数量
                                                            }
                                                            dt_.Rows.Add(dr.ItemArray);
                                                        }
                                                    }
                                                }
                                                else //每一行存在多个板这种格式
                                                {
                                                    int FPackedNo = Convert.ToInt16(str2[1].ToString()) - Convert.ToInt16(str2[0].ToString()) + 1; //板数
                                                    string newFCartonNo = dr["FCartonNo"].ToString();
                                                    int newFQty = Convert.ToInt16(dr["FQty"].ToString());
                                                    int new_FQty = Convert.ToInt16(dr["_FQty"].ToString());
                                                    double FNW = Convert.ToDouble(dr["FNW"].ToString());
                                                    double FGW = Convert.ToDouble(dr["FGW"].ToString());
                                                    string[] str = Regex.Split(newFCartonNo.Replace("@", "").Trim(), "-", RegexOptions.IgnoreCase); //第一种箱号为1-16 
                                                    int FCartonNo = Convert.ToInt16(str[1].ToString()) - Convert.ToInt16(str[0].ToString());
                                                    //循环板号
                                                    for (int a = Convert.ToInt16(str2[0].ToString()); a <= Convert.ToInt16(str2[1].ToString()); a++)
                                                    {

                                                        if (newFCartonNo.Contains("-"))
                                                        {

                                                            //进行拆板分箱
                                                            for (int i = Convert.ToInt16(str[0].ToString()); i <= Convert.ToInt16(str[1].ToString()); i++)
                                                            {
                                                                dr["FPackedNo"] = a;//板号
                                                                dr["FCartonNo"] = i;//箱号
                                                                if (FCartonNo == 0)
                                                                {
                                                                    dr["FQty"] = new_FQty; //每一箱数量
                                                                    dr["_FQty"] = new_FQty;
                                                                    dr["_FNW"] = FNW; //每一箱数量
                                                                    dr["_FGW"] = FGW;
                                                                    dr["FUnit"] = FUnitID;
                                                                }
                                                                else
                                                                {
                                                                    dr["FUnit"] = FUnitID;
                                                                    dr["FQty"] = newFQty / ((FCartonNo + 1) * FPackedNo); //每一箱数量
                                                                    dr["_FQty"] = newFQty / ((FCartonNo + 1) * FPackedNo);
                                                                    dr["_FNW"] = FNW / ((FCartonNo + 1) * FPackedNo); //每箱净重
                                                                    dr["_FGW"] = FGW / ((FCartonNo + 1) * FPackedNo); //每箱毛重
                                                                    dr["FNW"] = FNW / FPackedNo; //单板净重
                                                                    dr["FGW"] = FGW / FPackedNo; //单板毛重

                                                                }
                                                                dt_.Rows.Add(dr.ItemArray);
                                                            }
                                                        }
                                                    }

                                                }
                                            }
                                            #endregion

                                        }
                                        else if (CustomKey == "1002") //「手机」PACKING引入
                                        {
                                            #region  手机拆分 

                                            if (dr["FPackedNo"].ToString().Contains("-")) //每一行只有一板号
                                            {
                                                //去掉-
                                                string[] str_ = Regex.Split(dr["FPackedNo"].ToString().Replace("@", "").Trim(), "-", RegexOptions.IgnoreCase);
                                                //去掉/
                                                string[] str1 = Regex.Split(str_[0].ToString(), "/", RegexOptions.IgnoreCase);
                                                //去掉/
                                                string[] str2 = Regex.Split(str_[1].ToString(), "/", RegexOptions.IgnoreCase);
                                                //每箱数量
                                                if (!string.IsNullOrEmpty(dr_item["FDXQTY"].ToString())&& Convert.ToDouble(dr_item["FDXQTY"])>0)
                                                {
                                                   
                                                    if (str1[0].ToString() == str2[0].ToString()) //这里一行只有一板
                                                    {
                                                        int TatalFPackedNoQty = Convert.ToInt32(dr["FQty"].ToString());
                                                        double SingleFCartonNo = Convert.ToDouble(dr_item["FDXQTY"]); //物料表获取单箱数量
                                                        int TotalFCartonNo = Convert.ToInt32(Math.Floor(TatalFPackedNoQty / SingleFCartonNo));
                                                        double TotalNW= string.IsNullOrEmpty(dr["FNW"].ToString())?0:Convert.ToDouble(dr["FNW"].ToString()); //总净重
                                                        double TotalGW = string.IsNullOrEmpty(dr["FGW"].ToString()) ? 0 : Convert.ToDouble(dr["FGW"].ToString()); //总毛重
                                                        //每单位cut重量
                                                        double NWUnitweight = TotalNW > 0 ? TatalFPackedNoQty / TotalNW : 0;
                                                        //每单位cut重量
                                                        double GWUnitweight = TotalGW > 0 ? TatalFPackedNoQty / TotalGW : 0;
                                                        //剩余数量
                                                        double RemainQty = TatalFPackedNoQty % SingleFCartonNo;
                                                        if (TotalFCartonNo == 0)
                                                        {
                                                                dr["FPackedNo"] = str1[0].ToString();//板号
                                                                dr["FCartonNo"] = 1;//箱号
                                                                dr["FQty"] = TatalFPackedNoQty; //每一箱数量
                                                                dr["_FQty"] = TatalFPackedNoQty;
                                                                dr["FUnit"] = FUnitID;
                                                                dr["_FNW"] = TatalFPackedNoQty * NWUnitweight; //每箱净重
                                                                dr["_FGW"] = TatalFPackedNoQty * GWUnitweight; //每箱毛重
                                                                dt_.Rows.Add(dr.ItemArray);
                                                        }
                                                        else
                                                        {
                                                            //循环箱数
                                                            for (int i = 1; i <= TotalFCartonNo; i++)
                                                            {
                                                                dr["FPackedNo"] = str1[0].ToString();//板号
                                                                dr["FCartonNo"] = i;//箱号
                                                                dr["FUnit"] = FUnitID;
                                                                dr["FQty"] = SingleFCartonNo; //每一箱数量
                                                                dr["_FQty"] = SingleFCartonNo;
                                                                dr["_FNW"] = SingleFCartonNo * NWUnitweight; //每箱净重
                                                                dr["_FGW"] = SingleFCartonNo * GWUnitweight; //每箱毛重
                                                                dt_.Rows.Add(dr.ItemArray);

                                                            }
                                                            if (RemainQty > 0)
                                                            {
                                                                //剩余数量
                                                                dr["FPackedNo"] = str1[0].ToString();//板号
                                                                dr["FCartonNo"] = TotalFCartonNo + 1;//箱号
                                                                dr["FUnit"] = FUnitID;
                                                                dr["FQty"] = RemainQty; //每一箱数量
                                                                dr["_FQty"] = RemainQty;
                                                                dr["_FNW"] = RemainQty * NWUnitweight; //每箱净重
                                                                dr["_FGW"] = RemainQty * GWUnitweight; //每箱毛重
                                                                dt_.Rows.Add(dr.ItemArray);
                                                            }
                                                        }
                                                      
                                                    }
                                                    else //存在多个板
                                                    {
                                                        //str1[0].ToString() == str2[0].ToString()
                                                        int a = Convert.ToInt32(str1[0].ToString());
                                                        int  b= Convert.ToInt32(str2[0].ToString());
                                                        int qty = b - a + 1;
                                                        for (int j = a; j <= b; j++)
                                                        {
                                                            int TatalFPackedNoQty = Convert.ToInt32(dr["FQty"].ToString())/ qty;
                                                            double SingleFCartonNo = Convert.ToDouble(dr_item["FDXQTY"]); //物料表获取单箱数量
                                                            int TotalFCartonNo = Convert.ToInt32(Math.Floor(TatalFPackedNoQty / SingleFCartonNo));
                                                            double TotalNW = string.IsNullOrEmpty(dr["FNW"].ToString()) ? 0 : Convert.ToDouble(dr["FNW"].ToString()); //总净重
                                                            double TotalGW = string.IsNullOrEmpty(dr["FGW"].ToString()) ? 0 : Convert.ToDouble(dr["FGW"].ToString()); //总毛重
                                                            //每单位cut重量
                                                            double NWUnitweight = TotalNW>0?TatalFPackedNoQty / TotalNW:0;
                                                            //每单位cut重量
                                                            double GWUnitweight = TotalGW > 0 ? TatalFPackedNoQty / TotalGW:0;
                                                            //剩余数量
                                                            double RemainQty = TatalFPackedNoQty % SingleFCartonNo;

                                                            //循环箱数
                                                            for (int i = 1; i <= TotalFCartonNo; i++)
                                                            {
                                                                dr["FPackedNo"] = j;//板号
                                                                dr["FCartonNo"] = i;//箱号
                                                                dr["FQty"] = SingleFCartonNo; //每一箱数量
                                                                dr["_FQty"] = SingleFCartonNo;
                                                                dr["FUnit"] = FUnitID;
                                                                dr["_FNW"] = SingleFCartonNo * NWUnitweight; //每箱净重
                                                                dr["_FGW"] = SingleFCartonNo * GWUnitweight; //每箱毛重
                                                                dt_.Rows.Add(dr.ItemArray);

                                                            }
                                                            if (RemainQty > 0)
                                                            {
                                                                //剩余数量
                                                                dr["FPackedNo"] = str1[0].ToString();//板号
                                                                dr["FCartonNo"] = TotalFCartonNo + 1;//箱号
                                                                dr["FQty"] = SingleFCartonNo; //每一箱数量
                                                                dr["FUnit"] = FUnitID;
                                                                dr["_FQty"] = SingleFCartonNo;
                                                                dr["_FNW"] = SingleFCartonNo * NWUnitweight; //每箱净重
                                                                dr["_FGW"] = SingleFCartonNo * GWUnitweight; //每箱毛重
                                                                dt_.Rows.Add(dr.ItemArray);
                                                            }


                                                        }

                                                    }
                                                    
                                                }
                                                else //未维护单箱换算率
                                                {
                                                    err_row += "第【" + m + "】行分录，物料代码【" + dr["FPartID"].ToString() + "】未维护单箱数量，换算失败。\r\n";
                                                    continue;
                                                }
                                               
                                            }

                                           #endregion
                                        }
                                       
                                        continue;
                                    }
                                    else //存在源单
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
                                result += "\r\n" + err_row;
                                result += "\r\n________________________________________________________________________\r\n";
                                continue;
                            }
                            if (dt.Rows.Count > 0)
                            {
                                #region 新增单据
                                IMetaDataService mService = Kingdee.BOS.App.ServiceHelper.GetService<IMetaDataService>();
                                FormMetadata meta = mService.Load(this.Context, "STK_InStock") as FormMetadata;

                                BusinessInfo info = meta.BusinessInfo;
                                IResourceServiceProvider formServiceProvider = meta.BusinessInfo.GetForm().GetFormServiceProvider(true);
                                IBillViewService billViewService = formServiceProvider.GetService(typeof(IDynamicFormView)) as IBillViewService;

                                IBillView bill_view = (IBillView)billViewService;

                                DynamicObject leavebill = new DynamicObject(meta.BusinessInfo.GetDynamicObjectType());
                                //表头
                                string BillTypevalue = "0a2c1694596d440882adb080a7a8ca1b";
                                BaseDataField BillType = meta.BusinessInfo.GetField("FBillTypeID") as BaseDataField;
                                IViewService viewService = Kingdee.BOS.App.ServiceHelper.GetService<IViewService>();
                                DynamicObject[] djlx = viewService.LoadFromCache(this.Context, new object[] { BillTypevalue }, BillType.RefFormDynamicObjectType);
                                BillType.RefIDDynamicProperty.SetValue(leavebill, BillTypevalue);
                                BillType.DynamicProperty.SetValue(leavebill, djlx[0]);
                                //业务类型BusinessType
                                leavebill["BusinessType"] = "CG";
                                //作废状态
                                leavebill["CancelStatus"] = "A";
                                //库存组织
                                BaseDataField FSTOCKORGID = meta.BusinessInfo.GetField("FSTOCKORGID") as BaseDataField;
                                leavebill["StockOrgId_Id"] = Convert.ToString(this.Context.CurrentOrganizationInfo.ID);
                                leavebill["StockOrgId"] = viewService.LoadSingle(this.Context, this.Context.CurrentOrganizationInfo.ID, FSTOCKORGID.RefFormDynamicObjectType);
                                //需求组织
                                BaseDataField FDEMANDORGID = meta.BusinessInfo.GetField("FDEMANDORGID") as BaseDataField;
                                leavebill["DemandOrgId_Id"] = Convert.ToString(this.Context.CurrentOrganizationInfo.ID);
                                leavebill["DemandOrgId"] = viewService.LoadSingle(this.Context, this.Context.CurrentOrganizationInfo.ID, FDEMANDORGID.RefFormDynamicObjectType);

                                //创建日期
                                leavebill["CreateDate"] = DateTime.Now;
                                //日期
                                leavebill["Date"] = analysis[2].ToString();
                                leavebill["OwnerTypeIdHead"] = "BD_OwnerOrg";
                                //货主
                                BaseDataField FOWNERID = meta.BusinessInfo.GetField("FOWNERID") as BaseDataField;
                                leavebill["OwnerIdHead_Id"] = Convert.ToString(this.Context.CurrentOrganizationInfo.ID);
                                leavebill["OwnerIdHead"] = viewService.LoadSingle(this.Context, this.Context.CurrentOrganizationInfo.ID, FOWNERID.RefFormDynamicObjectType);
                                //采购组织

                                BaseDataField FPURCHASEORGID = meta.BusinessInfo.GetField("FPURCHASEORGID") as BaseDataField;
                                leavebill["PurchaseOrgId_Id"] = Convert.ToString(this.Context.CurrentOrganizationInfo.ID);
                                leavebill["PurchaseOrgId"] = viewService.LoadSingle(this.Context, this.Context.CurrentOrganizationInfo.ID, FPURCHASEORGID.RefFormDynamicObjectType);
                                //供应商
                                BaseDataField FSUPPLIERID2 = meta.BusinessInfo.GetField("FSUPPLIERID") as BaseDataField;
                                leavebill["SupplierId_Id"] = Convert.ToString(FSUPPLIERID);
                                leavebill["SupplierId"] = viewService.LoadSingle(this.Context, FSUPPLIERID, FSUPPLIERID2.RefFormDynamicObjectType);
                                //供货方
                                BaseDataField FSUPPLYID = meta.BusinessInfo.GetField("FSUPPLYID") as BaseDataField;
                                leavebill["SupplyId_Id"] = Convert.ToString(FSUPPLIERID);
                                leavebill["SupplyId"] = viewService.LoadSingle(this.Context, FSUPPLIERID, FSUPPLYID.RefFormDynamicObjectType);
                                //结算方
                                BaseDataField FSETTLEID = meta.BusinessInfo.GetField("FSETTLEID") as BaseDataField;
                                leavebill["SettleId_Id"] = Convert.ToString(FSUPPLIERID);
                                leavebill["SettleId"] = viewService.LoadSingle(this.Context, FSUPPLIERID, FSETTLEID.RefFormDynamicObjectType);

                                //收款方
                                BaseDataField FCHARGEID = meta.BusinessInfo.GetField("FCHARGEID") as BaseDataField;
                                leavebill["ChargeId_Id"] = Convert.ToString(FSUPPLIERID);
                                leavebill["ChargeId"] = viewService.LoadSingle(this.Context, FSUPPLIERID, FCHARGEID.RefFormDynamicObjectType);

                                //Invoice No
                                leavebill["F_PAEZ_Text"] = analysis[1].ToString();

                                //财务信息InStockFin
                                DynamicObjectCollection EntityRows = leavebill["InStockFin"] as DynamicObjectCollection;
                                Entity InStockFin = meta.BusinessInfo.GetEntity("FInStockFin"); //分录
                                DynamicObject EntityRow = new DynamicObject(InStockFin.DynamicObjectType);

                                //结算组织
                                BaseDataField FSETTLEORGID = meta.BusinessInfo.GetField("FSETTLEORGID") as BaseDataField;
                                EntityRow["SettleOrgId_Id"] = Convert.ToString(this.Context.CurrentOrganizationInfo.ID);
                                EntityRow["SettleOrgId"] = viewService.LoadSingle(this.Context, this.Context.CurrentOrganizationInfo.ID, FSETTLEORGID.RefFormDynamicObjectType);
                                //结算币别SettleCurrId
                                BaseDataField FSETTLECURRID = meta.BusinessInfo.GetField("FSETTLECURRID") as BaseDataField;
                                EntityRow["SettleCurrId_Id"] = Convert.ToString(7);
                                EntityRow["SettleCurrId"] = viewService.LoadSingle(this.Context, 7, FSETTLECURRID.RefFormDynamicObjectType);
                                //汇率
                                EntityRow["ExchangeRate"] = FEXCHANGERATE;
                                //本位币
                                BaseDataField FLOCALCURRID = meta.BusinessInfo.GetField("FLOCALCURRID") as BaseDataField;
                                EntityRow["LocalCurrId_Id"] = 1;
                                EntityRow["LocalCurrId"] = viewService.LoadSingle(this.Context, 1, FLOCALCURRID.RefFormDynamicObjectType);
                                //汇率类型
                                BaseDataField FEXCHANGETYPEID = meta.BusinessInfo.GetField("FEXCHANGETYPEID") as BaseDataField;
                                EntityRow["ExchangeTypeId_Id"] = 1;
                                EntityRow["ExchangeTypeId"] = viewService.LoadSingle(this.Context, 1, FEXCHANGETYPEID.RefFormDynamicObjectType);
                                //定价时点
                                EntityRow["PriceTimePoint"] = 1;
                                EntityRows.Add(EntityRow);

                                //表体数据包
                                DynamicObjectCollection leaveEntityRows = leavebill["InStockEntry"] as DynamicObjectCollection;
                                Entity cbtjdEntry = meta.BusinessInfo.GetEntity("FInStockEntry"); //分录
                                //sql_3 = "select  max(FENTRYID) from  T_STK_INSTOCKENTRY";
                                //long maxFID = DBServiceHelper.ExecuteScalar<Int64>(this.Context, sql_3, 0, null);
                                for (int i = 0; i < dt_.Rows.Count; i++)
                                {

                                    DataRow dr_item = dt_Item.Rows.Find(dt_.Rows[i]["FPartID"].ToString());
                                    //新增一行Convert.ToInt32(dr_item["FMASTERID"])
                                    DynamicObject newLeaveEntityRow = new DynamicObject(cbtjdEntry.DynamicObjectType);
                                    //newLeaveEntityRow
                                    newLeaveEntityRow["Seq"] = i + 1;
                                    //newLeaveEntityRow["Id"] = maxFID+i+1;
                                    //物料
                                    BaseDataField FMATERIALID = meta.BusinessInfo.GetField("FMATERIALID") as BaseDataField;
                                    newLeaveEntityRow["MaterialId_Id"] = Convert.ToInt32(dr_item["FMASTERID"]);
                                    newLeaveEntityRow["MaterialId"] = viewService.LoadSingle(this.Context, Convert.ToInt32(dr_item["FMASTERID"]), FMATERIALID.RefFormDynamicObjectType);
                                    //采购单位

                                    BaseDataField FREMAININSTOCKUNITID = meta.BusinessInfo.GetField("FREMAININSTOCKUNITID") as BaseDataField;
                                    newLeaveEntityRow["RemainInStockUnitId_Id"] = Convert.ToString(dt_.Rows[i]["FUnit"].ToString());
                                    newLeaveEntityRow["RemainInStockUnitId"] = viewService.LoadSingle(this.Context, dt_.Rows[i]["FUnit"].ToString(), FREMAININSTOCKUNITID.RefFormDynamicObjectType);

                                    //基本单位
                                    BaseDataField FBASEUNITID = meta.BusinessInfo.GetField("FBASEUNITID") as BaseDataField;
                                    newLeaveEntityRow["BaseUnitID_Id"] = Convert.ToString(dt_.Rows[i]["FUnit"].ToString());
                                    newLeaveEntityRow["BaseUnitID"] = viewService.LoadSingle(this.Context, dt_.Rows[i]["FUnit"].ToString(), FBASEUNITID.RefFormDynamicObjectType);
                                    //计价单位
                                    BaseDataField FPRICEUNITID = meta.BusinessInfo.GetField("FPRICEUNITID") as BaseDataField;
                                    newLeaveEntityRow["PriceUnitID_Id"] = Convert.ToString(dt_.Rows[i]["FUnit"].ToString());
                                    newLeaveEntityRow["PriceUnitID"] = viewService.LoadSingle(this.Context, dt_.Rows[i]["FUnit"].ToString(), FPRICEUNITID.RefFormDynamicObjectType);
                                    //辅单位
                                    BaseDataField FEXTAUXUNITID = meta.BusinessInfo.GetField("FEXTAUXUNITID") as BaseDataField;
                                    newLeaveEntityRow["ExtAuxUnitId_Id"] = Convert.ToString(dt_.Rows[i]["FUnit"].ToString());
                                    newLeaveEntityRow["ExtAuxUnitId"] = viewService.LoadSingle(this.Context, dt_.Rows[i]["FUnit"].ToString(), FEXTAUXUNITID.RefFormDynamicObjectType);
                                    //库存状态
                                    BaseDataField FSTOCKSTATUSID = meta.BusinessInfo.GetField("FSTOCKSTATUSID") as BaseDataField;
                                    newLeaveEntityRow["StockStatusId_Id"] = 10000;
                                    newLeaveEntityRow["StockStatusId"] = viewService.LoadSingle(this.Context, 10000, FSTOCKSTATUSID.RefFormDynamicObjectType);
                                    //仓库
                                    BaseDataField FSTOCKID = meta.BusinessInfo.GetField("FSTOCKID") as BaseDataField;
                                    newLeaveEntityRow["StockId_Id"] = 106064;
                                    newLeaveEntityRow["StockId"] = viewService.LoadSingle(this.Context, 106064, FSTOCKID.RefFormDynamicObjectType);
                                    //税率(%)
                                    newLeaveEntityRow["TaxRate"] = 16;
                                    //含税单价
                                    newLeaveEntityRow["TaxPrice"] = 10;
                                    //库存单位
                                    BaseDataField FUNITID = meta.BusinessInfo.GetField("FUNITID") as BaseDataField;
                                    newLeaveEntityRow["UnitID_Id"] = Convert.ToString(dt_.Rows[i]["FUnit"].ToString());
                                    newLeaveEntityRow["UnitID"] = viewService.LoadSingle(this.Context, dt_.Rows[i]["FUnit"].ToString(), FUNITID.RefFormDynamicObjectType);
                                    //货主类型
                                    newLeaveEntityRow["OWNERTYPEID"] = "BD_OwnerOrg";
                                    //货主
                                    BaseDataField FOWNERID2 = meta.BusinessInfo.GetField("FOWNERID") as BaseDataField;
                                    newLeaveEntityRow["OWNERID_Id"] = Convert.ToString(this.Context.CurrentOrganizationInfo.ID);
                                    newLeaveEntityRow["OWNERID"] = viewService.LoadSingle(this.Context, this.Context.CurrentOrganizationInfo.ID, FOWNERID2.RefFormDynamicObjectType);
                                    //保管者类型
                                    newLeaveEntityRow["KeeperTypeID"] = "BD_OwnerOrg";
                                    //保管者类型
                                    BaseDataField FKEEPERID = meta.BusinessInfo.GetField("FKEEPERID") as BaseDataField;
                                    newLeaveEntityRow["KeeperID_Id"] = Convert.ToString(this.Context.CurrentOrganizationInfo.ID);
                                    newLeaveEntityRow["KeeperID"] = viewService.LoadSingle(this.Context, this.Context.CurrentOrganizationInfo.ID, FKEEPERID.RefFormDynamicObjectType);
                                    //产品类型
                                    newLeaveEntityRow["RowType"] = "Standard";
                                    //应付关闭状态
                                    newLeaveEntityRow["PayableCloseStatus"] = "A";
                                    //采购数量
                                    newLeaveEntityRow["RemainInStockQty"] = Convert.ToDecimal(dt_.Rows[i]["FQty"]);
                                    //采购基本数量
                                    newLeaveEntityRow["RemainInStockBaseQty"] = Convert.ToDecimal(dt_.Rows[i]["FQty"]);
                                    //Lot
                                    // newLeaveEntityRow["Lot_Id"] = 205556;
                                    // newLeaveEntityRow["Lot"] = 205556;
                                    newLeaveEntityRow["Lot_Text"] = analysis[1].ToString() + "_" + dt_.Rows[i]["FPackedNo"].ToString() + "_" + dt_.Rows[i]["FCartonNo"].ToString();
                                    //实收数量
                                    newLeaveEntityRow["RealQty"] = Convert.ToDecimal(dt_.Rows[i]["FQty"]);
                                    //实收数量(辅单位)
                                    newLeaveEntityRow["ExtAuxUnitQty"] = Convert.ToDecimal(dt_.Rows[i]["FQty"]);
                                    //计价数量
                                    newLeaveEntityRow["PriceUnitQty"] = Convert.ToDecimal(dt_.Rows[i]["FQty"]);
                                    // 应收数量
                                    newLeaveEntityRow["MustQty"] = Convert.ToDecimal(dt_.Rows[i]["FQty"]);
                                    ////配货数量(基本单位)
                                    //newLeaveEntityRow["FAllotBaseQty"] = Convert.ToDecimal(dt_.Rows[i]["FQty"]);
                                    ////关联数量(基本单位)
                                    //newLeaveEntityRow["BaseJoinQty"] = Convert.ToDecimal(dt_.Rows[i]["FQty"]);
                                    ////消耗汇总基本单位数量
                                    //newLeaveEntityRow["BaseConsumeSumQty"] = Convert.ToDecimal(dt_.Rows[i]["FQty"]);
                                    //库存基本数量
                                    newLeaveEntityRow["BaseUnitQty"] = Convert.ToDecimal(dt_.Rows[i]["FQty"]);
                                    newLeaveEntityRow["F_PAEZ_Text1"] = dt_.Rows[i]["FPackedNo"].ToString();
                                    newLeaveEntityRow["F_PAEZ_Text2"] = dt_.Rows[i]["FCartonNo"].ToString();
                                    newLeaveEntityRow["F_PAEZ_Decimal"] = Convert.ToDecimal(dt_.Rows[i]["_FQty"]);
                                    newLeaveEntityRow["F_PAEZ_Decimal1"] = Convert.ToDecimal(dt_.Rows[i]["FNW"]);
                                    newLeaveEntityRow["F_PAEZ_Decimal2"] = Convert.ToDecimal(dt_.Rows[i]["FGW"]);
                                    newLeaveEntityRow["F_PAEZ_Decimal3"] = Convert.ToDecimal(dt_.Rows[i]["_FNW"]);

                                    newLeaveEntityRow["F_PAEZ_Decimal4"] = Convert.ToDecimal(dt_.Rows[i]["_FGW"]);
                                    newLeaveEntityRow["F_PAEZ_Text3"] = dt_.Rows[i]["FMEAS"].ToString();
                                    leaveEntityRows.Add(newLeaveEntityRow);
                                }

                                //cbtjdlist.Add(leavebill);
                                //保存
                                IOperationResult SaveResult = Operation2(leavebill, meta);
                                string msg = "";
                                if (!SaveResult.IsSuccess)
                                {
                                    foreach (var item in SaveResult.ValidationErrors)
                                    {
                                        result += result + item.Message;
                                    }
                                    if (!SaveResult.InteractionContext.IsNullOrEmpty())
                                    {
                                        result += result + SaveResult.InteractionContext.SimpleMessage;
                                    }
                                }
                                else
                                {
                                    foreach (var item in SaveResult.OperateResult)
                                    {
                                        result += result + item.Message;
                                        string fnmber = item.Number;
                                        sql_3 = "update t_STK_InStock set  FOBJECTTYPEID='STK_InStock'  where  FBILLNO='" + fnmber + "'";
                                        DBServiceHelper.Execute(this.Context, sql_3);

                                    }
                                }
                                // Operation(cbtjd, cbtjdlist);

                                #endregion

                                #region 方法二： 创建视图、模型，模拟手工新增，会触发大部分的表单服务和插件
                                //FormMetadata meta = MetaDataServiceHelper.Load(this.Context, "STK_InStock") as FormMetadata;
                                //BusinessInfo info = meta.BusinessInfo;
                                //IResourceServiceProvider formServiceProvider = meta.BusinessInfo.GetForm().GetFormServiceProvider(true);
                                //IBillViewService billViewService = formServiceProvider.GetService(typeof(IDynamicFormView)) as IBillViewService;

                                //Form form = meta.BusinessInfo.GetForm();
                                //BillOpenParameter billOpenParameter = new BillOpenParameter(form.Id, meta.GetLayoutInfo().Id);
                                //billOpenParameter = new BillOpenParameter(form.Id, string.Empty);
                                //billOpenParameter.Context = this.Context;
                                //billOpenParameter.ServiceName = form.FormServiceName;
                                //billOpenParameter.PageId = Guid.NewGuid().ToString();
                                //billOpenParameter.FormMetaData = meta;
                                //billOpenParameter.LayoutId = meta.GetLayoutInfo().Id;
                                //billOpenParameter.Status = OperationStatus.ADDNEW;
                                //billOpenParameter.PkValue = null;
                                //billOpenParameter.CreateFrom = CreateFrom.Default;
                                //billOpenParameter.ParentId = 0;
                                //billOpenParameter.GroupId = "";
                                //billOpenParameter.DefaultBillTypeId = null;
                                //billOpenParameter.DefaultBusinessFlowId = null;
                                //billOpenParameter.SetCustomParameter("ShowConfirmDialogWhenChangeOrg", false);
                                //List<AbstractDynamicFormPlugIn> value = form.CreateFormPlugIns();
                                //billOpenParameter.SetCustomParameter(FormConst.PlugIns, value);

                                //((IDynamicFormViewService)billViewService).Initialize(billOpenParameter, formServiceProvider);

                                //IBillView bill_view = (IBillView)billViewService;

                                //bill_view.CreateNewModelData();

                                //DynamicFormViewPlugInProxy proxy = bill_view.GetService<DynamicFormViewPlugInProxy>();
                                //proxy.FireOnLoad();

                                //bill_view.Model.SetItemValueByID("FStockOrgId", this.Context.CurrentOrganizationInfo.ID, 0);
                                //bill_view.Model.SetValue("FDate", analysis[2].ToString());
                                //bill_view.Model.SetItemValueByNumber("FBillTypeID", "RKD01_SYS", 0);
                                //bill_view.Model.SetValue("FOwnerTypeIdHead", "BD_OwnerOrg");
                                //bill_view.Model.SetItemValueByID("FOwnerIdHead", this.Context.CurrentOrganizationInfo.ID, 0);
                                //bill_view.Model.SetItemValueByID("FPurchaseOrgId", this.Context.CurrentOrganizationInfo.ID, 0);
                                //bill_view.Model.SetItemValueByID("FSettleOrgId", this.Context.CurrentOrganizationInfo.ID, 0);
                                //bill_view.Model.SetItemValueByID("FSupplierId", FSUPPLIERID, 0);
                                //bill_view.Model.SetValue("F_PAEZ_Text", analysis[1].ToString(), 0);


                                //bill_view.Model.SetItemValueByNumber("FLocalCurrID", "PRE001", 0);
                                //bill_view.Model.SetItemValueByNumber("FSettleCurrId", "PRE007", 0);

                                //bill_view.Model.SetValue("FExchangeRate", FEXCHANGERATE);
                                //bill_view.Model.SetItemValueByNumber("FExchangeTypeID", "HLTX01_SYS", 0);
                                //bill_view.Model.SetValue("FPriceTimePoint", 1);

                                //for (int i = 0; i < dt_.Rows.Count; i++)
                                //{
                                //    bill_view.Model.CreateNewEntryRow("FInStockEntry");
                                //    if (dt_.Rows[i]["FSrcBillNo"] != null && dt_.Rows[i]["FSrcBillNo"].ToString() != "")
                                //    {
                                //        bill_view.Model.SetValue("FSRCBILLTYPEID", "PUR_PurchaseOrder", i);
                                //        bill_view.Model.SetValue("FSRCBILLNO", dt_.Rows[i]["FSrcBillNo"].ToString(), i);
                                //        bill_view.Model.SetValue("FPOORDERENTRYID", Convert.ToInt32(dt_.Rows[i]["FSrcEntryID"]), i);
                                //        bill_view.Model.SetValue("FPOORDERNO", dt_.Rows[i]["FSrcBillNo"].ToString(), i);
                                //    }

                                //    bill_view.Model.SetItemValueByNumber("FMATERIALID", dt_.Rows[i]["FPartID"].ToString(), i);
                                //    //库存单位
                                //    //bill_view.Model.SetValue("FUnitID", dt_.Rows[i]["FUnit"], i);
                                //    //bill_view.Model.SetValue("FBASEUNITID", dt_.Rows[i]["FUnit"], i);
                                //    bill_view.InvokeFieldUpdateService("FMATERIALID", i);
                                //    bill_view.InvokeFieldUpdateService("FUNITID", i);
                                //    bill_view.InvokeFieldUpdateService("FBASEUNITID", i);
                                //    bill_view.Model.SetValue("FREALQTY", Convert.ToDecimal(dt_.Rows[i]["FQty"]), i);
                                //    bill_view.InvokeFieldUpdateService("FREALQTY", i);
                                //    bill_view.Model.SetValue("FMustQty", Convert.ToDecimal(dt_.Rows[i]["FMustQty"]), i);
                                //    bill_view.Model.SetValue("F_PAEZ_Text1", dt_.Rows[i]["FPackedNo"].ToString(), i);
                                //    bill_view.Model.SetValue("F_PAEZ_Text2", dt_.Rows[i]["FCartonNo"].ToString(), i);
                                //    bill_view.Model.SetValue("F_PAEZ_Decimal", Convert.ToDecimal(dt_.Rows[i]["_FQty"]), i);
                                //    bill_view.Model.SetValue("F_PAEZ_Decimal1", Convert.ToDecimal(dt_.Rows[i]["FNW"]), i);
                                //    bill_view.Model.SetValue("F_PAEZ_Decimal2", Convert.ToDecimal(dt_.Rows[i]["FGW"]), i);
                                //    bill_view.Model.SetValue("F_PAEZ_Decimal3", Convert.ToDecimal(dt_.Rows[i]["_FNW"]), i);
                                //    bill_view.Model.SetValue("F_PAEZ_Decimal4", Convert.ToDecimal(dt_.Rows[i]["_FGW"]), i);
                                //    bill_view.Model.SetValue("F_PAEZ_Text3", dt_.Rows[i]["FMEAS"].ToString(), i);
                                //}

                                //if (bill_view.InvokeFormOperation("GenerateLotByCodeRule"))
                                //{
                                //    IOperationResult save_result = bill_view.Model.Save();
                                //    if (save_result.IsSuccess)
                                //    {
                                //        string fid = string.Empty;
                                //        string Fnumber = string.Empty;
                                //        OperateResultCollection Collection = save_result.OperateResult;
                                //        foreach (var item in Collection)
                                //        {
                                //            fid = item.PKValue.ToString();
                                //            Fnumber = item.Number.ToString();
                                //        }
                                //        Import = true;
                                //        result += "\r\n引入成功！生成采购入库单号为：" + Fnumber + "\r\n________________________________________________________________________\r\n";
                                //        continue;
                                //    }
                                //    else
                                //    {
                                //        for (int mf = 0; mf < save_result.ValidationErrors.Count; mf++)
                                //        {
                                //            result += "\r\n" + save_result.ValidationErrors[mf].Message;
                                //        }
                                //        result += "\r\n________________________________________________________________________\r\n";
                                //        continue;
                                //    }
                                //}
                                //else
                                //{
                                //    result += "\r\n获取批号错误！\r\n________________________________________________________________________\r\n";
                                //    continue;
                                //}

                                #endregion
                            }
                            else
                            {
                                result = "读取数据失败txt格式错误!";
                            }

                        }
                        catch (Exception ex)
                        {
                            result += "\r\n" + ex.Message.ToString() + "\r\n________________________________________________________________________\r\n";
                            continue;
                        }
                    }
                    this._FileList.Clear();
                    this.FileNameList.Clear();
                    this.View.GetControl("F_JD_FileUpdate").SetValue(DBNull.Value);
                    this.View.Refresh();
                    this.View.ShowMessage("导入完成,以下为具体的引入情况：" + result, MessageBoxType.Advise);
                }
            }
            else if (e.Key.EqualsIgnoreCase("F_JD_BTNCancel"))
            {
                this.View.ReturnToParentWindow(new FormResult(Import));
                this.View.Close();
            }
        }


       /// <summary>
       /// 调用保存操作
       /// </summary>
       /// <param name="bussinessInfo"></param>
       /// <param name="saveList"></param>
       /// <param name="isall"></param>
        private void Operation(BusinessInfo bussinessInfo, List<DynamicObject> saveList, bool isall = true)
        {
            DBServiceHelper.LoadReferenceObject(this.Context, saveList.ToArray(), bussinessInfo.GetDynamicObjectType(), false);
            if (saveList.Count == 0) return;
            IOperationResult result = BusinessDataServiceHelper.Save(this.Context, bussinessInfo, saveList.ToArray());
            if (!result.IsSuccess)
            {
                this.View.ShowOperateResult(result.OperateResult);
                return;
            }
        }
        /// <summary>
        /// 校验操作
        /// </summary>
        /// <param name="result"></param>
        /// <param name="msg"></param>
        private void CheckResult(IOperationResult result, string msg)
        {
            if (!result.IsSuccess)
            {
                foreach (var item in result.ValidationErrors)
                {
                    msg = msg + item.Message;
                }
                if (!result.InteractionContext.IsNullOrEmpty())
                {
                    msg = msg + result.InteractionContext.SimpleMessage;
                }

                throw new KDException("", msg);
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


        /// <summary>
        /// 平板格式读取txt 文件(带箱数)
        /// </summary>
        /// <param name="fileLoad"></param>
        /// <returns></returns>
        private List<object> GetAnalysisTxt(string fileLoad)
        {
            StreamReader rd = new StreamReader(fileLoad, Encoding.GetEncoding("gb2312"));
            List<object> ret = new List<object> { };
            string line;
            int ii = 0; //记录读取的
            string FSupplierName = "";
            string FDate = "";
            string PackingInvoiceNo = "";
            int Line = 0;
            string ParkNo = "";
            int datano = 0; //记录当前行

            DataTable dt = new DataTable();
            dt.Columns.Add("FPackedNo", typeof(string)); //板号
            dt.Columns.Add("FCartonNo", typeof(string));//箱号
            dt.Columns.Add("FPartID", typeof(string));
            dt.Columns.Add("FQty", typeof(decimal)); //总数
            dt.Columns.Add("FUnit", typeof(string));
            dt.Columns.Add("FNW", typeof(decimal)); //净重
            dt.Columns.Add("FGW", typeof(decimal)); //毛重
            dt.Columns.Add("FMEAS", typeof(string));
            dt.Columns.Add("_FQty", typeof(decimal)); //单箱数
            dt.Columns.Add("_FNW", typeof(decimal));
            dt.Columns.Add("_FGW", typeof(decimal));
            dt.Columns.Add("FSrcBillNo", typeof(string));
            dt.Columns.Add("FSrcEntryID", typeof(int));
            dt.Columns.Add("FMustQty", typeof(decimal));

            DataRow dr = dt.NewRow();
            int jiedian = 9;
            #region  旧循环行
            //while ((line = rd.ReadLine()) != null)
            //{
            //    if (ii == 1)
            //        FSupplierName = line.Trim();

            //    if (ii == 9 && line.Trim() == "Packing List")
            //        jiedian = 10;

            //    if (ii == jiedian)
            //    {
            //        FDate = line.Substring(line.IndexOf("Date:") + 5, line.Length - line.IndexOf("Date:") - 5);
            //        PackingInvoiceNo = line.Substring(19, line.Length - line.IndexOf("Date:") + 19).Replace(" ", "");
            //    }
                 
            //    if (ii >= jiedian + 20)
            //    {
            //        if (line.Contains("----------"))
            //            break;

            //        if (line.Trim() != "" && line.Substring(0, 18).Trim() != "")
            //        {
            //            string ss = "";
            //            if (line.Contains("*"))
            //            {
            //                ss = line.Trim().Substring(line.Trim().IndexOf("*"), line.Trim().Length - line.Trim().IndexOf("*"));
            //                ss = ss.Replace(" ", "");
            //                line = line.Substring(0, line.Trim().IndexOf("*") - 1) + ss;
            //            }

            //            string[] strs = line.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            //            string finallStr = string.Join(" ", strs);

            //            string[] data = finallStr.Split(' ');

            //            //版号
            //            if (line.Substring(0, 9).Trim() != "")
            //            {
            //                dr["FPackedNo"] = ParkNo = data[1];
            //                dr["FCartonNo"] = data[2];
            //                dr["FQty"] = Convert.ToDecimal(data[4]);
            //                dr["FUnit"] = data[5];
            //                dr["FNW"] = data.Length < 7 ? 0 : Convert.ToDecimal(data[6]);
            //                dr["FGW"] = data.Length < 8 ? 0 : Convert.ToDecimal(data[7]);
            //                dr["FMEAS"] = data.Length < 9 ? "" : data[8];
            //            }
            //            else
            //            {
            //                dr["FPackedNo"] = ParkNo;
            //                dr["FCartonNo"] = data[0];
            //                dr["FQty"] = Convert.ToDecimal(data[2]);
            //                dr["FUnit"] = data[3];
            //                dr["FNW"] = data.Length < 5 ? 0 : Convert.ToDecimal(data[4]);
            //                dr["FGW"] = data.Length < 6 ? 0 : Convert.ToDecimal(data[5]);
            //                dr["FMEAS"] = data.Length < 7 ? "" : data[6];
            //            }

            //            Line = ii;
            //        }

            //        if (Line + 1 == ii)
            //        {
            //            string[] kks = line.Split('@');
            //            dr["_FQty"] = Convert.ToDecimal(kks[1].Replace("/CTN", "").Trim());
            //            dr["_FNW"] = Convert.ToDecimal(kks[2].Trim());
            //            if (kks.Length <= 3)
            //                dr["_FGW"] = 0;
            //            else
            //                dr["_FGW"] = Convert.ToDecimal(kks[3].Trim());

            //        }
            //        if (Line + 2 == ii)
            //        {
            //            dr["FPartID"] = line.Split(',')[2].Trim();
            //            dt.Rows.Add(dr.ItemArray);
            //        }
            //    }

            //    ii++;
            //}
            #endregion
            #region  循环行 新
            while ((line = rd.ReadLine()) != null)
            {
                if(line.Trim() != "")
                {
                    //第二行为供应商名称
                    if (ii == 1)
                        FSupplierName = line.Trim();
                    //h获取日期和Invoice 号
                    if (line.Contains("Invoice No") && line.Contains("Date"))
                    {
                        FDate = line.Substring(line.IndexOf("Date:") + 5, line.Length - line.IndexOf("Date:") - 5);
                        PackingInvoiceNo = line.Substring(19, line.Length - line.IndexOf("Date:") + 19).Replace(" ", "");
                    }

                    if (line.Contains("T Packed  Carton   Innolux Part ID"))
                    {
                        //记录当前行
                        datano = ii;
                    }
                    //开始读取数据
                    if (datano != 0 && ii == datano + 3)
                    {
                        if (!line.Contains("---------------"))
                        {
                            if (line.Trim() != "" && line.Substring(0, 18).Trim() != "")
                            {
                                string ss = "";
                                if (line.Contains("*"))
                                {
                                    ss = line.Trim().Substring(line.Trim().IndexOf("*"), line.Trim().Length - line.Trim().IndexOf("*"));
                                    ss = ss.Replace(" ", "");
                                    //line = line.Substring(0, line.Trim().IndexOf("*") - 1) + ss;
                                    line = line.Substring(0, line.Trim().IndexOf("*")) + ss;
                                }

                                string[] strs = line.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                string finallStr = string.Join(" ", strs);

                                string[] data = finallStr.Split(' ');

                                // //第一种
                                if (data.Length==8)
                                {
                                    dr["FPackedNo"] = ParkNo = data[1];
                                    dr["FCartonNo"] = data[2];
                                    dr["FQty"] = Convert.ToDecimal(data[3]);
                                    dr["FUnit"] = data[4];
                                    dr["FNW"] = data.Length < 7 ? 0 : Convert.ToDecimal(data[5]);
                                    dr["FGW"] = data.Length < 8 ? 0 : Convert.ToDecimal(data[6]);
                                    dr["FMEAS"] = data.Length < 9 ? "" : data[7];

                                }
                                if (data.Length == 4) //多箱组合
                                {
                                    dr["FCartonNo"] = data[0];
                                    dr["FQty"] = Convert.ToDecimal(data[1]);
                                    dr["FUnit"] = data[2];
                                    dr["FNW"] = data.Length < 4 ? 0 : Convert.ToDecimal(data[3]);
                                    dr["FGW"] = 0;
                                    dr["FMEAS"] = 0;
                                }
                                if (data.Length == 5&&!line.Contains("@")) //多箱组合
                                {
                                    dr["FCartonNo"] = data[0];
                                    dr["FQty"] = Convert.ToDecimal(data[2]);
                                    dr["FUnit"] = data[3];
                                    dr["FNW"] = Convert.ToDecimal(data[4]);
                                    dr["FGW"] = 0;
                                    dr["FMEAS"] = 0;
                                }
                                //第二种
                                if (data.Length == 11)
                                {
                                    dr["FPackedNo"] = ParkNo = data[1];
                                    dr["FCartonNo"] = data[2];
                                    dr["FQty"] = Convert.ToDecimal(data[4]);
                                    dr["FUnit"] = data[5];
                                    dr["FNW"] = data.Length < 7 ? 0 : Convert.ToDecimal(data[6]);
                                    dr["FGW"] = data.Length < 8 ? 0 : Convert.ToDecimal(data[7]);
                                    dr["FMEAS"] = data.Length < 9 ? "" : data[8];
                                }
                                //第三种格式
                                if (data.Length == 9)
                                {
                                    //dr["FPackedNo"] = ParkNo = data[1];
                                    //dr["FCartonNo"] = data[2];
                                    //dr["FQty"] = Convert.ToDecimal(data[2]);
                                    //dr["FUnit"] = data[3];
                                    //dr["FNW"] = data.Length < 5 ? 0 : Convert.ToDecimal(data[4]);
                                    //dr["FGW"] = data.Length < 6 ? 0 : Convert.ToDecimal(data[5]);
                                    //dr["FMEAS"] = data.Length < 7 ? "" : data[6];
                                    dr["FPackedNo"] = ParkNo = data[1];
                                    dr["FCartonNo"] = data[2];
                                    dr["FQty"] = Convert.ToDecimal(data[4]);
                                    dr["FUnit"] = data[5];
                                    dr["FNW"] = Convert.ToDecimal(data[6]);
                                    dr["FGW"] =  Convert.ToDecimal(data[7]);
                                    dr["FMEAS"] =data[8];
                                }

                                Line = ii;
                            }

                            if (Line + 1 == ii)
                            {
                                string[] kks = line.Split('@');
                                dr["_FQty"] = Convert.ToDecimal(kks[1].Replace("/CTN", "").Trim());
                                dr["_FNW"] = Convert.ToDecimal(kks[2].Trim());
                                if (kks.Length <= 3)
                                    dr["_FGW"] = 0;
                                else
                                    dr["_FGW"] = Convert.ToDecimal(kks[3].Trim());

                            }
                            if (Line + 2 == ii)
                            {
                                dr["FPartID"] = line.Split(',')[2].Trim();
                                dt.Rows.Add(dr.ItemArray);
                            }
                            datano++;
                        }

                    }
                    ii++;
                }
               
            }
            #endregion
            ret.Add(FSupplierName);
            ret.Add(PackingInvoiceNo);
            ret.Add(FDate);
            ret.Add(dt);
            rd.Close();
            return ret;
        }

        /// <summary>
        /// 手机格式的txt文件(不带箱数)
        /// </summary>
        /// <param name="FileLoad"></param>
        /// <returns></returns>
        private List<object> GetAnalysisTxt2(string FileLoad)
        {
            StreamReader rd = new StreamReader(FileLoad, Encoding.GetEncoding("gb2312"));
            List<object> ret = new List<object> { };
            string line;
            int ii = 0;
            string FSupplierName = "";
            string FDate = "";
            string PackingInvoiceNo = "";
            int Line = 0;
            string ParkNo = "";
            int datano = 0; //记录当前行
            DataTable dt = new DataTable();
            dt.Columns.Add("FPackedNo", typeof(string)); //板号
            dt.Columns.Add("FCartonNo", typeof(string));//箱号
            dt.Columns.Add("FPartID", typeof(string));
            dt.Columns.Add("FQty", typeof(decimal)); //总数
            dt.Columns.Add("FUnit", typeof(string));
            dt.Columns.Add("FNW", typeof(decimal)); //净重
            dt.Columns.Add("FGW", typeof(decimal)); //毛重
            dt.Columns.Add("FMEAS", typeof(string));
            dt.Columns.Add("_FQty", typeof(decimal)); //单箱数
            dt.Columns.Add("_FNW", typeof(decimal));
            dt.Columns.Add("_FGW", typeof(decimal));
            dt.Columns.Add("FSrcBillNo", typeof(string));
            dt.Columns.Add("FSrcEntryID", typeof(int));
            dt.Columns.Add("FMustQty", typeof(decimal));

            DataRow dr = dt.NewRow();
            int _Line = 0;
            #region 旧的读取方式
            //while ((line = rd.ReadLine()) != null)
            //{
            //    if (ii == 0)
            //        FSupplierName = line.Trim();

            //    if (ii == 7)
            //    {
            //        FDate = line.Substring(line.IndexOf("DATE:") + 5, line.Length - line.IndexOf("DATE:") - 5).Trim();
            //        PackingInvoiceNo = line.Substring(0, 87).Replace("INVOICE NO:", "").Trim();
            //    }

            //    if (line.Contains("TOTAL:"))
            //        break;

            //    if (ii >= _Line)
            //    {

            //        if (line.Contains("----------") || line.Trim() == "")
            //            continue;
            //        if (line.Contains("Page:"))
            //        {
            //            _Line = ii + 22;
            //            continue;
            //        }

            //        if (line.Trim() != "" && line.Substring(0, 18).Trim() != "")
            //        {
            //            Line = ii;

            //            dr["FPackedNo"] = line.Substring(0, 18).Trim();
            //            string[] b = line.Replace("@","").Split(new Char[2] { ' ', ' ' });
            //            List<string> strList = new List<string> { };
            //            foreach (string New in b)
            //            {
            //                if (New.Trim() != "")
            //                {
            //                    strList.Add(New.Trim());
            //                }
            //            }
            //            if (!line.Contains("@"))
            //            {
            //                dr["FQty"] = Convert.ToDecimal(strList[2]);
            //                dr["FNW"] = Convert.ToDecimal(strList[4]);
            //                dr["FGW"] = strList.Count >= 7 ? Convert.ToDecimal(strList[6]) : 0;
            //                dr["FMEAS"] = strList.Count >= 9 ? strList[8] : "";
            //            }
            //            else
            //            {
            //                dr["FQty"] = 0;
            //                dr["FMEAS"] = strList.Count >= 6 ? strList[5] : "";
            //            }
            //        }

            //        if (Line + 1 == ii)
            //        {
            //            if (Convert.ToDecimal(dr["FQty"]) == 0 && line.Trim() != "")
            //            {
            //                string[] b = line.Split(new Char[2] { ' ', ' ' });
            //                List<string> strList = new List<string> { };
            //                foreach (string New in b)
            //                {
            //                    if (New.Trim() != "")
            //                    {
            //                        strList.Add(New.Trim());
            //                    }
            //                }
            //                dr["FQty"] = Convert.ToDecimal(strList[1]);
            //                dr["FNW"] = Convert.ToDecimal(strList[3]);
            //                dr["FGW"] = strList.Count >= 6 ? Convert.ToDecimal(strList[5]) : 0;
            //            }
            //        }
            //        if (Line + 2 == ii)
            //        {
            //            dr["FPartID"] = line.Trim();
            //            dt.Rows.Add(dr.ItemArray);
            //        }
            //    }

            //    ii++;
            //}
            #endregion
            #region  循环行 新
            while ((line = rd.ReadLine()) != null)
            {
                //第二行为供应商名称
                if (ii == 0)
                    FSupplierName = line.Trim();
                //h获取日期和Invoice 号
                if (line.Contains("INVOICE NO") && line.Contains("DATE"))
                {
                    FDate = line.Substring(line.IndexOf("DATE:") + 5, line.Length - line.IndexOf("DATE:") - 5);

                    int  rr = line.IndexOf("DATE:");
                    PackingInvoiceNo = line.Substring(12, line.Length - line.IndexOf("DATE:")+12).Replace(" ", "");
                }

                if (line.Contains("Description")&&line.Contains("Pack"))
                {
                    //记录当前行
                    datano = ii;
                }
                //开始读取数据
                if (datano != 0 && ii == datano + 2)
                {
                    if (!line.Contains("---------------"))
                    {
                        if (line.Trim() != "")
                        {
                            string ss = "";
                            if (line.Contains("*"))
                            {
                                ss = line.Trim().Substring(line.Trim().IndexOf("*"), line.Trim().Length - line.Trim().IndexOf("*"));
                                ss = ss.Replace(" ", "");
                                line = line.Substring(0, line.Trim().IndexOf("*")) + ss;
                            }

                            string[] strs = line.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            string finallStr = string.Join(" ", strs);

                            string[] data = finallStr.Split(' ');

                            //版号
                            if (data.Length==9)
                            {
                                dr["FPackedNo"] = ParkNo = data[0];
                                dr["FCartonNo"] = 0;
                                dr["FQty"] = Convert.ToDecimal(data[2]);
                                dr["FUnit"] = data[3];
                                dr["FNW"] = string.IsNullOrEmpty(data[4].ToString() )? 0 : Convert.ToDecimal(data[4]);
                                dr["FGW"] = string.IsNullOrEmpty(data[6].ToString()) ? 0 : Convert.ToDecimal(data[6]);
                                dr["FMEAS"] = data[8];
                                Line = ii;
                            }
                            //版号
                            if (data.Length == 6)
                            {
                                dr["FPackedNo"] = ParkNo = data[0];
                                dr["FCartonNo"] = 0;
                                dr["FQty"] = Convert.ToDecimal(data[2]);
                                dr["FUnit"] = data[3];
                                Line = ii;
                            }
                            //第二种格式
                            
                            //版号
                            if (data.Length == 10)
                            {
                                if (line.Contains("@"))
                                {
                                    dr["FPackedNo"] = ParkNo = data[0];
                                    dr["FCartonNo"] = 0;
                                    dr["FMEAS"] = data[9];
                                    Line = ii;
                                }
                                else
                                {
                                    dr["FPackedNo"] = ParkNo = data[0];
                                    dr["FCartonNo"] = 0;
                                    dr["FQty"] = Convert.ToDecimal(data[3]);
                                    dr["FUnit"] = data[4];
                                    dr["FNW"] = string.IsNullOrEmpty(data[5].ToString()) ? 0 : Convert.ToDecimal(data[5]);
                                    dr["FGW"] = string.IsNullOrEmpty(data[7].ToString()) ? 0 : Convert.ToDecimal(data[7]);
                                    dr["FMEAS"] = data[9];
                                    Line = ii;
                                }
                                   
                            }
                            if (Line + 1 == ii)
                            {
                                if (data.Length == 7)
                                {
                                    dr["FQty"] = Convert.ToDecimal(data[1]);
                                    dr["FUnit"] = data[2];
                                    dr["FNW"] = string.IsNullOrEmpty(data[3].ToString()) ? 0 : Convert.ToDecimal(data[3]);
                                    dr["FGW"] = string.IsNullOrEmpty(data[5].ToString()) ? 0 : Convert.ToDecimal(data[5]);
                                   
                                }
                            }

                        }
                       
                        if (Line + 2 == ii)
                        {
                            dr["FPartID"] = line.Trim();
                            dt.Rows.Add(dr.ItemArray);
                        }
                        datano++;
                    }

                }
                ii++;
            }
            #endregion

            ret.Add(FSupplierName);
            ret.Add(PackingInvoiceNo);
            ret.Add(FDate);
            ret.Add(dt);
            rd.Close();
            return ret;
        }
    }
}
