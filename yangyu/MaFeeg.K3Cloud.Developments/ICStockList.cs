using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS;
using Kingdee.K3.MFG.App;
using Kingdee.BOS.Core.List.PlugIn;
using System.Data;
using System.ComponentModel;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Metadata;

namespace MaFeeg.K3Cloud.Developments
{
    [Description("按钮事件")]
    [Kingdee.BOS.Util.HotUpdate]
    public class ICStockList : AbstractListPlugIn
    {
        /// <summary>
        /// 按钮事件
        /// </summary>
        /// <param name="e"></param>
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);
            if (e.BarItemKey == "tb_ImportFileUpdateEdit")
            {
                DynamicFormShowParameter showParam = new DynamicFormShowParameter();
                showParam.OpenStyle.ShowType = ShowType.Modal;
                showParam.FormId = "PAEZ_ICStockBillExproint";
                showParam.CustomParams.Add("CustomKey", "1001");
                this.View.ShowForm(showParam,
                    new Action<FormResult>((formResult) =>
                    {
                        if (formResult != null && formResult.ReturnData != null)
                        {
                            bool Import = (bool)formResult.ReturnData;
                            if (Import)
                            { 
                                //刷新list
                                this.ListView.RefreshByFilter();
                            }
                        }
                    }));
            }

            if (e.BarItemKey == "tb_ImportFileUpdateEdit_Phone")
            {
                DynamicFormShowParameter showParam = new DynamicFormShowParameter();
                showParam.OpenStyle.ShowType = ShowType.Modal;
                showParam.FormId = "PAEZ_ICStockBillExproint";
                showParam.CustomParams.Add("CustomKey", "1002");
                this.View.ShowForm(showParam,
                    new Action<FormResult>((formResult) =>
                    {
                        if (formResult != null && formResult.ReturnData != null)
                        {
                            bool Import = (bool)formResult.ReturnData;
                            if (Import)
                            {
                                this.ListView.RefreshByFilter();
                            }
                        }
                    }));
            }

            if (e.BarItemKey == "tb_ImportFileUpdateEdit_2")
            {
                DynamicFormShowParameter showParam = new DynamicFormShowParameter();
                showParam.OpenStyle.ShowType = ShowType.Modal;
                showParam.FormId = "PAEZ_ICStockBillExproint_2";
                showParam.CustomParams.Add("CustomKey", "2001");

                this.View.ShowForm(showParam,
                    new Action<FormResult>((formResult) =>
                    {
                        if (formResult != null && formResult.ReturnData != null)
                        {
                            bool Import = (bool)formResult.ReturnData;
                            if (Import)
                            {
                                this.ListView.RefreshByFilter();
                            }
                        }
                    }));
            }

            if (e.BarItemKey == "tb_ImportFileUpdateEdit_2_Phone")
            {
                DynamicFormShowParameter showParam = new DynamicFormShowParameter();
                showParam.OpenStyle.ShowType = ShowType.Modal;
                showParam.FormId = "PAEZ_ICStockBillExproint_2";
                showParam.CustomParams.Add("CustomKey", "2002");

                this.View.ShowForm(showParam,
                    new Action<FormResult>((formResult) =>
                    {
                        if (formResult != null && formResult.ReturnData != null)
                        {
                            bool Import = (bool)formResult.ReturnData;
                            if (Import)
                            {
                                this.ListView.RefreshByFilter();
                            }
                        }
                    }));
            }

            if (e.BarItemKey == "btn_Excel")
            {
                DynamicFormShowParameter showParam = new DynamicFormShowParameter();
                showParam.OpenStyle.ShowType = ShowType.Modal;
                showParam.FormId = "PAEZ_EXCEL";
                showParam.CustomParams.Add("CustomKey", "3001");

                this.View.ShowForm(showParam,
                    new Action<FormResult>((formResult) =>
                    {
                        if (formResult != null && formResult.ReturnData != null)
                        {
                            bool Import = (bool)formResult.ReturnData;
                            if (Import)
                            {
                                this.ListView.RefreshByFilter();
                            }
                        }
                    }));
            }

            //导入转换率按钮
            if (e.BarItemKey == "ImportRateBtn")
            {
                DynamicFormShowParameter showParam = new DynamicFormShowParameter();
                showParam.OpenStyle.ShowType = ShowType.Modal;
                showParam.FormId = "PAEZ_RATE_EXCEL";
                this.View.ShowForm(showParam,
                    new Action<FormResult>((formResult) =>
                    {
                        if (formResult != null && formResult.ReturnData != null)
                        {
                            bool Import = (bool)formResult.ReturnData;
                            if (Import)
                            {
                                this.ListView.RefreshByFilter();
                            }
                        }
                    }));
            }
            //拣货查询
            if (e.BarItemKey == "tbPicking")
            {
                Context context = this.Context;
                DynamicFormShowParameter showParam = new DynamicFormShowParameter();
                showParam.OpenStyle.ShowType = ShowType.Modal;
                showParam.FormId = "PAEZ_PickQuery";
                this.View.ShowForm(showParam,
                    new Action<FormResult>((formResult) =>
                    {

                        if (formResult != null && formResult.ReturnData != null)
                        {
                            object result = formResult.ReturnData;
                        
                            ReturnInfo returnInfo = (ReturnInfo)formResult.ReturnData;  //数量

                            CalCulateOrderPlan calc = new CalCulateOrderPlan();

                            //Msg msg = Profits.IntoCostUnrealizedProfits(FConsolidationSchemeID, riqi, context);

                           ReturnParam returnParam = calc.GenerateSolutions(returnInfo, context);//生成方案

                            if (returnParam.status)
                            {
                                BillShowParameter param = new BillShowParameter();
                                param.FormId = returnParam.FBIZFORMID;
                                //param.Status = OperationStatus.ADDNEW;
                                param.Status = OperationStatus.EDIT;
                                param.PKey = returnParam.FBUSINESSCODE;
                                this.View.ShowForm(param);

                            }
                            else
                            {
                                this.View.ShowErrMessage(returnParam.msg);
                                this.View.Refresh();
                            }



                        }

                    }));
            }

        }
    }
}
