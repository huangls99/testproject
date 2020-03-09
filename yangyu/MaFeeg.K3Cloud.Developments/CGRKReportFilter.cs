using Kingdee.BOS.Core.CommonFilter.PlugIn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaFeeg.K3Cloud.Developments
{
    [System.ComponentModel.Description("采购入库单报表的过滤框插件")]
    [Kingdee.BOS.Util.HotUpdate]
   public class CGRKReportFilter : AbstractCommonFilterPlugIn
   {

        public override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            //组织
            string FOrganization = this.Model.GetValue("FOrganization").ToString();
            //仓库
            string FWarehouse = this.Model.GetValue("FWarehouse").ToString();
            //仓库
            string FMATERIALID = this.Model.GetValue("FMATERIALID").ToString();
            //设置默认值
            if (this.Model.GetValue("FStartDate").ToString() == "")
                this.Model.SetValue("FStartDate", "2019-1-1");

            if (this.Model.GetValue("FEndDate").ToString() == "")
                this.Model.SetValue("FEndDate", DateTime.Now);

            //this.View.Refresh();

        }
        //支持多选
        //public override void ButtonClick(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.ButtonClickEventArgs e)
        //{
        //    base.ButtonClick(e);
        //    if (e.Key.Equals("Fbtn"))
        //    {
        //        ListShowParameter listPara = new ListShowParameter();
        //        listPara.FormId = "BD_Metarial";
        //        listPara.IsLookUp = true;  //查找型列表
        //        listPara.MultiSelect = true;  //支持多选
        //        this.View.ShowForm(listPara, new Action<FormResult>((result) =>
        //        {
        //            //回调函数里面处理数据回填等逻辑
        //            if (result.ReturnData == null)
        //            {
        //                return;
        //            }
        //        })
        //        );
        //    }
        //}


    }
}
