using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.JSON;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.ServiceHelper.Excel;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace MaFeeg.K3Cloud.Developments
{
    [Description("EXCEL转换率导入")]
    [Kingdee.BOS.Util.HotUpdate]
    public class ImportConvertsionRate : AbstractDynamicFormPlugIn
    {

        private List<string> FileNameList = new List<string> { };
        private List<string> _FileList = new List<string> { };
        private bool Import = false;

        /// <summary>
        /// 锁定确定按钮
        /// </summary>
        /// <param name="e"></param>
        public override void AfterBindData(EventArgs e)
        {
            this.View.GetControl("F_JD_BTNOK").Enabled = false;
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
        ///点击确定按钮
        /// </summary>
        /// <param name="e"></param>
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            if (e.Key.EqualsIgnoreCase("F_JD_BTNOK"))
            {

                try
                {

                    this.View.GetControl("F_JD_BTNOK").Enabled = false;
                    if (FileNameList.Count < 1)
                    {
                        this.View.ShowMessage("未检测到需要引入的excel文件！", MessageBoxType.Error);
                    }
                    else
                    {
                        ///读取excel到Dataset
                        DataSet dss_1;
                        string sql = string.Empty;
                        string updatesql = string.Empty;
                        string err_row = "";
                        using (ExcelOperation helper = new ExcelOperation(this.View))
                        {
                            dss_1 = helper.ReadFromFile(FileNameList[0], 0, 0);
                        }
                        //获取当前所有物料
                        sql = "select FMASTERID, FNUMBER FROM T_BD_MATERIAL";
                        DataSet ds = DBServiceHelper.ExecuteDataSet(this.Context, sql);
                        DataTable dt_Item = ds.Tables[0];
                        dt_Item.PrimaryKey = new DataColumn[] { dt_Item.Columns["FNUMBER"] }; //设置主键
                        DataTable dt_ex = dss_1.Tables[0];
                        //读取excel
                        for (int i = 1; i < dt_ex.Rows.Count; i++)
                        {
                            //物料编码
                            string WlNumber = dt_ex.Rows[i][3].ToString();
                            //单位编码
                            string DWFnumber = dt_ex.Rows[i][5].ToString();
                            //pcs换算率
                            int Fdate = string.IsNullOrEmpty(dt_ex.Rows[i][7].ToString()) ? 0 : Convert.ToInt32(dt_ex.Rows[i][7].ToString());
                            //单箱数量
                            int Dxqty = string.IsNullOrEmpty(dt_ex.Rows[i][8].ToString()) ? 0 : Convert.ToInt32(dt_ex.Rows[i][8].ToString());

                            //判断是否存在改物料

                            DataRow dr_item = dt_Item.Rows.Find(dt_ex.Rows[i][3].ToString());
                           
                            if (dr_item == null)
                            {
                                err_row += "第【" + i + "】行分录，物料代码【" + dt_ex.Rows[i][3].ToString() + "】不存在。\r\n";
                                continue;
                            }
                            else
                            {
                                if (DWFnumber == "PCS")
                                {
                                    Fdate = 1;
                                }
                                updatesql += "/*dialect*/  update T_BD_MATERIAL set FDXQTY=" + Dxqty + ",FPCSCONVERT=" + Fdate + " where FNUMBER='" + WlNumber + "';";

                            }
                        }
                        //更新
                        int reult = DBServiceHelper.Execute(this.Context, updatesql);
                        this.View.ShowMessage("更新成功 \r\n" + err_row.ToString(), MessageBoxType.AskOK);
                        this._FileList.Clear();
                        this.FileNameList.Clear();
                        this.View.GetControl("F_JD_FileUpdate").SetValue(DBNull.Value);
                        this.View.Refresh();
                        return;


                    }
                }
                catch(Exception ex)
                {
                    this.View.ShowMessage("更新换算率失败："+ ex.ToString(), MessageBoxType.Error);
                    this._FileList.Clear();
                    this.FileNameList.Clear();
                    this.View.GetControl("F_JD_FileUpdate").SetValue(DBNull.Value);
                    this.View.Refresh();
                    return;
                }

            }
        }

    }
}
