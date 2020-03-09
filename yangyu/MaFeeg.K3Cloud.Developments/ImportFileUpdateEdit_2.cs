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
using System.ComponentModel;

namespace MaFeeg.K3Cloud.Developments
{
    [Description("INVOICE文件导入")]
    [Kingdee.BOS.Util.HotUpdate]
    public class ImportFileUpdateEdit_2 : AbstractDynamicFormPlugIn
    {
        private List<string> FileNameList = new List<string> { };
        private List<string> _FileList = new List<string> { };
        private bool Import = false;
        public override void AfterBindData(EventArgs e)
        {
            this.View.GetControl("F_JD_BTNOK").Enabled = false;
            string CustomKey = this.View.OpenParameter.GetCustomParameter("CustomKey").ToString();
            if (CustomKey == "2001")
            {
                LocaleValue str = new LocaleValue("「平板」INVOICE 引入");
                this.View.SetFormTitle(str);
            }
            else if (CustomKey == "2002")
            {
                LocaleValue str = new LocaleValue("「手机」INVOICE 引入");
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
 
                    JSONObject uploadInfo = KDObjectConverter.DeserializeObject<JSONObject>(e.EventArgs);
                    if (uploadInfo != null)
                    {
                        JSONArray json = new JSONArray(uploadInfo["NewValue"].ToString());
                        if (json.Count < 1)
                        {
                            this.View.GetControl("F_JD_BTNOK").Enabled = false;
                        }
                        else
                        {
                            for (int i = 0; i < json.Count; i++)
                            {
                                string fileName = (json[i] as Dictionary<string, object>)["ServerFileName"].ToString();
                                string _FileName = (json[i] as Dictionary<string, object>)["FileName"].ToString();
                                FileNameList.Add(this.GetFullFileName(fileName));
                                _FileList.Add(_FileName);
                            }

                            this.View.GetControl("F_JD_BTNOK").Enabled = true;
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
                    for (int f = 0; f < FileNameList.Count; f++)
                    {
                        result += "\r\n《" + _FileList[f] + "》结果:";
                        try
                        {
                            string CustomKey = this.View.OpenParameter.GetCustomParameter("CustomKey").ToString();//获取父级页面传参的参数
                            List<object> analysis = new List<object> { };
                            if (CustomKey == "2001")
                                analysis = GetAnalysisTxt(FileNameList[f]);
                            else
                                analysis = GetAnalysisTxt2(FileNameList[f]);

                            string sql_3 = @"select tt.FID,t1.FENTRYID,t2.FMATERIALID,t2.FNUMBER from t_STK_InStock tt
                            left outer join T_STK_INSTOCKENTRY t1 on tt.FID=t1.FID
                            left outer join T_BD_MATERIAL t2 on t1.FMATERIALID=t2.FMATERIALID
                            where tt.FCancelStatus='A' and tt.FDocumentStatus in('A','B') and tt.F_PAEZ_Text='" + analysis[0].ToString() + "'";


                            DataSet ds_3 = DBServiceHelper.ExecuteDataSet(this.Context, sql_3);
                            DataTable dt_head = ds_3.Tables[0]; dt_head.PrimaryKey = new DataColumn[] { dt_head.Columns["FENTRYID"] };
                            if (dt_head.Rows.Count < 1)
                            {
                                result += "\r\n 不存在Invoice No：“" + analysis[0].ToString() + "”的单据或已提交或已审核,无法更新单价。\r\n________________________________________________________________________\r\n";
                                continue;
                            }
                            long BillID = Convert.ToInt64(dt_head.Rows[0]["FID"]);
                            DataTable dt_entry = (DataTable)analysis[1];

                            #region 修改单据数据

                            FormMetadata meta = MetaDataServiceHelper.Load(this.Context, "STK_InStock") as FormMetadata;
                            BusinessInfo info = meta.BusinessInfo;
                            DynamicObject toModifyObj = Kingdee.BOS.ServiceHelper.BusinessDataServiceHelper.LoadSingle(this.Context, BillID, info.GetDynamicObjectType());

                            if (toModifyObj != null)
                            {
                                decimal FEXCHANGERATE = 1;
                                DynamicObjectCollection InStockFin = toModifyObj["InStockFin"] as DynamicObjectCollection;
                                foreach (DynamicObject StockFin in InStockFin)
                                {
                                    FEXCHANGERATE = Convert.ToDecimal(info.GetField("FEXCHANGERATE").DynamicProperty.GetValue(StockFin));//汇率
                                }

                                DynamicObjectCollection entryObjs = toModifyObj["InStockEntry"] as DynamicObjectCollection;
                                string FNUMBER = string.Empty;
                                decimal FTaxPrice = 0; decimal FTAXRATE = 0; decimal FPRICEUNITQTY = 0;
                                string err_row = "";
                                foreach (DynamicObject entryObj in entryObjs)
                                {
                                    int FENTRYID = Convert.ToInt32(entryObj["Id"]);
                                    FTAXRATE = Convert.ToDecimal(info.GetField("FTAXRATE").DynamicProperty.GetValue(entryObj)) / 100;
                                    FPRICEUNITQTY = Convert.ToDecimal(info.GetField("FPRICEUNITQTY").DynamicProperty.GetValue(entryObj));
                                    DataRow dr = dt_head.Rows.Find(FENTRYID);
                                    if (dr != null)
                                    {
                                        FNUMBER = dr["FNUMBER"].ToString();
                                        DataRow[] dr_r = dt_entry.Select("FPartID='" + FNUMBER + "'");
                                        if (dr_r.Length > 0)
                                        {
                                            FTaxPrice = Convert.ToDecimal(dr_r[0]["FUnitPrice"]);
                                            decimal FPRICE = Math.Round((FTaxPrice / (1 + FTAXRATE)), 6, MidpointRounding.AwayFromZero);
                                            decimal FALLAMOUNT = Math.Round((FTaxPrice * FPRICEUNITQTY), 2, MidpointRounding.AwayFromZero);
                                            decimal FAMOUNT = Math.Round((FTaxPrice / (1 + FTAXRATE) * FPRICEUNITQTY), 2, MidpointRounding.AwayFromZero);

                                            info.GetField("FPRICE").DynamicProperty.SetValue(entryObj, FPRICE);
                                            info.GetField("FTaxPrice").DynamicProperty.SetValue(entryObj, FTaxPrice);
                                            info.GetField("FALLAMOUNT").DynamicProperty.SetValue(entryObj, FALLAMOUNT);
                                            info.GetField("FAMOUNT").DynamicProperty.SetValue(entryObj, FAMOUNT);
                                            info.GetField("FTAXAMOUNT").DynamicProperty.SetValue(entryObj, FALLAMOUNT - FAMOUNT);
                                            info.GetField("FTAXNETPRICE").DynamicProperty.SetValue(entryObj, FTaxPrice);

                                            info.GetField("FTAXAMOUNT_LC").DynamicProperty.SetValue(entryObj, Math.Round((FALLAMOUNT - FAMOUNT) * FEXCHANGERATE, 2, MidpointRounding.AwayFromZero));//税额（本位币）
                                            info.GetField("FAMOUNT_LC").DynamicProperty.SetValue(entryObj, Math.Round((FAMOUNT * FEXCHANGERATE), 2, MidpointRounding.AwayFromZero));//金额（本位币）FAMOUNT_LC
                                            info.GetField("FALLAMOUNT_LC").DynamicProperty.SetValue(entryObj, Math.Round((FALLAMOUNT * FEXCHANGERATE), 2, MidpointRounding.AwayFromZero));//含税金额（本位币）FAMOUNT_LC                                              
                                        }
                                        else
                                        {
                                            err_row += "物料【" + FNUMBER + "】未检测到导入的数据。\r\n";
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        err_row += "分录【" + FENTRYID + "】未检测对应物料数据，请检查。\r\n";
                                        continue;
                                    }
                                }
                                if (err_row == "")
                                {
                                    IOperationResult save_result = Kingdee.BOS.ServiceHelper.BusinessDataServiceHelper.Save(this.Context, info, new DynamicObject[] { toModifyObj }, null, "Save");
                                    if (save_result.IsSuccess)
                                    {
                                        Import = true;
                                        result += "\r\n引入成功！\r\n________________________________________________________________________\r\n";
                                        continue;
                                    }
                                    else
                                    {
                                        for (int mf = 0; mf < save_result.ValidationErrors.Count; mf++)
                                        {
                                            result += "\r\n" + save_result.ValidationErrors[mf].Message;
                                        }
                                        result += "\r\n________________________________________________________________________\r\n";
                                        continue;
                                    }
                                }
                                else
                                {
                                    result += "\r\n" + err_row;
                                    result += "\r\n________________________________________________________________________\r\n";
                                    continue;
                                }
                            }
                            #endregion
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
         
        private List<object> GetAnalysisTxt(string fileLoad)
        { 
            StreamReader rd = new StreamReader(fileLoad, Encoding.GetEncoding("gb2312"));//台湾繁体BIG5解析
            List<object> ret = new List<object> { };
            string line; 
            int ii = 0;
            string PackingInvoiceNo = ""; 
            int _Line = int.MaxValue;

            DataTable dt = new DataTable();
          
            dt.Columns.Add("FPartID", typeof(string));
            dt.Columns.Add("FUnitPrice", typeof(decimal));
            int row=0;
            DataRow dr = dt.NewRow();
            #region 循环行
            while ((line = rd.ReadLine()) != null)
            {
                if (line.Contains("Shipping Invoice No:"))
                {
                    PackingInvoiceNo = line.Substring(20, line.Length - line.IndexOf("Date:") + 20).Replace(" ", "");
                }
                if (line.Contains("----------"))
                {
                    row++;
                    if (row == 4)
                    {
                        _Line = ii;
                    }
                }

                if (ii > _Line)
                {
                    if (line.Contains("----------"))
                        break;

                    if (line.Trim() != "" && line.Substring(0, 4).Trim() != "")
                    {
                        dr["FPartID"] = line.Substring(4, 23).Trim();
                        dr["FUnitPrice"] = Convert.ToDecimal(line.Substring(78, 18).Trim());
                        dt.Rows.Add(dr.ItemArray);
                    }
                }

                ii++;
            }
            #endregion
            ret.Add(PackingInvoiceNo);
            ret.Add(dt);
            rd.Close();
            return ret;
        }

        private List<object> GetAnalysisTxt2(string fileLoad)
        {
            StreamReader rd = new StreamReader(fileLoad, Encoding.GetEncoding("gb2312"));
            List<object> ret = new List<object> { };
            string line;
            int ii = 0;
            string PackingInvoiceNo = "";
            int Line = 0;

            DataTable dt = new DataTable();
            dt.Columns.Add("FPartID", typeof(string));
            dt.Columns.Add("FUnitPrice", typeof(decimal));
            DataRow dr = dt.NewRow();
            int _Line = 21;
            while ((line = rd.ReadLine()) != null)
            {
                if (ii == 7)
                {
                    PackingInvoiceNo = line.Substring(0, 87).Replace("INVOICE NO:", "").Trim();
                }

                if (line.Contains("TOTAL:"))
                    break;

                if (ii >= _Line)
                {

                    if (line.Contains("----------") || line.Trim() == "")
                        continue;
                    if (line.Contains("Page:"))
                    {
                        _Line = ii + 22;
                        continue;
                    }
 
                    if (line.Length > 116)
                    {
                        Line = ii;
                        dr["FUnitPrice"] = Convert.ToDecimal(line.Substring(85, 15).Trim().Replace(",", ""));
                    }

                    if (Line + 1 == ii)
                    {
                    }

                    if (Line + 2 == ii)
                    {
                        dr["FPartID"] = line.Trim();
                        dt.Rows.Add(dr.ItemArray);
                    }
                }
                ii++;
            }
            ret.Add(PackingInvoiceNo);
            ret.Add(dt);
            rd.Close();
            return ret;
        }
    }
}
