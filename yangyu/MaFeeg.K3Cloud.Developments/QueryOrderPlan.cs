using Kingdee.BOS;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MaFeeg.K3Cloud.Developments
{
    [Description("查询下单方案")]
    [Kingdee.BOS.Util.HotUpdate]
    public class QueryOrderPlan : AbstractDynamicFormPlugIn
    {

        /// <summary>
        /// 按钮点击事件
        /// </summary>
        /// <param name="e"></param>
        public override void ButtonClick(ButtonClickEventArgs e)
        {
            // 用户点击确定按钮
            if (e.Key.EqualsIgnoreCase("FQueryPlan"))
            {
                // 构建返回数据对象
                try
                {
                    Context context = this.Context;
                    ReturnInfo returnInfo = new ReturnInfo();
                    if (this.Model.GetValue("FMATERIALID") != null && this.Model.GetValue("FPickQty") != null)
                    {
                        returnInfo.Fnumber = ((DynamicObject)this.Model.GetValue("FMATERIALID"))["Id"].ToString();      //产品id
                        returnInfo.Qty = Convert.ToInt32(this.Model.GetValue("FPickQty"));     //起止日期
                        returnInfo.FGG = this.Model.GetValue("FGG").ToString();
                       // ReturnParam returnParam = GenerateSolutions(returnInfo.Fnumber, returnInfo.Qty,Context);//生成方案
                        // 把数据对象，返回给父界面
                        this.View.ReturnToParentWindow(new FormResult(returnInfo));
                        this.View.Close();
                    }
                    else
                    {
                        this.View.ShowErrMessage("请填写产品编码和数量!");
                    }
                }
                catch (Exception ex)
                {
                    this.View.ShowErrMessage("生成方案失败："+ ex.ToString());
                    throw;

                }

            }
        }

      

    }
}
