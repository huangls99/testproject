using Kingdee.BOS;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MaFeeg.K3Cloud.Developments
{
    [Description("生成方案")]
    [Kingdee.BOS.Util.HotUpdate]
    public  class CalCulateOrderPlan : AbstractListPlugIn
    {
        /// <summary>
        /// 生成方案
        /// </summary>
        /// <returns></returns>
        public ReturnParam GenerateSolutions(ReturnInfo returnInfo, Context context)
        {
            string sql = string.Empty;
            ReturnParam returnParam = new ReturnParam();
            try
            {
                string Fnumber = returnInfo.Fnumber;
                int qty = returnInfo.Qty;
                string dengji = returnInfo.FGG;
                //截取等级
                string[] array = Regex.Split(dengji, ",", RegexOptions.IgnoreCase);
                //等级
                List<Level> dj = new List<Level>()
                {
                      new Level(){Id="100002",FAUXPTYNUMBER="A"},
                      new Level(){Id="100003",FAUXPTYNUMBER = "B" },
                      new Level(){Id="100004",FAUXPTYNUMBER="F"},
                      new Level(){Id="100005",FAUXPTYNUMBER = "N" },
                      new Level(){Id="100006",FAUXPTYNUMBER="TB"},
                      new Level(){Id="100007",FAUXPTYNUMBER = "T" },
                      new Level(){Id="100008",FAUXPTYNUMBER="P"},
                      new Level(){Id="100009",FAUXPTYNUMBER="TB/B"},
                      new Level(){Id="100010",FAUXPTYNUMBER = "B/F" },
                      new Level(){Id="100011",FAUXPTYNUMBER="F/N"},
                };
                string sqldj = "";
                List<string> vs = new List<string>();
                for (int j = 0; j < array.Length; j++)
                {
                    Level level = dj.SingleOrDefault(p => p.FAUXPTYNUMBER == array[j].ToString());
                    if (level != null)
                    {
                        if (j == 0)
                        {
                            sqldj += "  t1.FMATERIALID=" + Fnumber + " and t1.FBASEQTY > 0 and t1.FAUXPROPID='" + level.Id + "' ";
                        }
                        else
                        {
                            sqldj += " or t1.FMATERIALID=" + Fnumber + " and t1.FBASEQTY > 0 and t1.FAUXPROPID='" + level.Id + "' ";
                        }

                    }
                }
                double PCSCONVERT = 1;
                double FDXQTY = 1;
                //获取库存数据
                sql = "select t1.FMATERIALID,t1.FLOT,t2.FNUMBER,t1.FBASEQTY, t4.FDATE ,t5.FPCSCONVERT,t5.FDXQTY,t1.FAUXPROPID   From  T_STK_INVENTORY t1" +
                     " left  join  T_BD_LOTMASTER t2 on t1.FLOT = t2.FLOTID" +
                    " left join T_STK_INSTOCKENTRY t3 on  t3.FLOT = t1.FLOT and t3.FMATERIALID = t1.FMATERIALID" +
                    " left join t_STK_InStock t4 on t3.FID = t4.FID" +
                    " left join  T_BD_MATERIAL t5 on t5.FMATERIALID=t1.FMATERIALID"+
                    " where ";
                string sqlend = " order by t4.FDATE asc";
                if (string.IsNullOrEmpty(dengji))
                {
                    sqldj= "t1.FMATERIALID=" + Fnumber + " and t1.FBASEQTY > 0 "+ sqlend;
                    sql = sql + sqldj;
                }
                else
                {
                    sql = sql + sqldj + sqlend;
                }
                DataSet ds = DBServiceHelper.ExecuteDataSet(context, sql);
                DataTable dt = ds.Tables[0];
                #region 方法二： 创建视图、模型，模拟手工新增，会触发大部分的表单服务和插件
                FormMetadata meta = MetaDataServiceHelper.Load(context, "PAEZ_OrderPlan") as FormMetadata;
                BusinessInfo info = meta.BusinessInfo;
                IResourceServiceProvider formServiceProvider = meta.BusinessInfo.GetForm().GetFormServiceProvider(true);
                IBillViewService billViewService = formServiceProvider.GetService(typeof(IDynamicFormView)) as IBillViewService;

                /******创建单据打开参数*************/
                Form form = meta.BusinessInfo.GetForm();
                BillOpenParameter billOpenParameter = new BillOpenParameter(form.Id, meta.GetLayoutInfo().Id);
                billOpenParameter = new BillOpenParameter(form.Id, string.Empty);
                billOpenParameter.Context = context;
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
                bill_view.Model.SetItemValueByID("FSaleOrgId", context.CurrentOrganizationInfo.ID, 0);
                bill_view.Model.SetValue("FDate", DateTime.Now.ToString());
                List<OrderPlan> orderPlans = new List<OrderPlan>();
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    OrderPlan order = new OrderPlan();
                    bill_view.Model.CreateNewEntryRow("FEntity");
                    bill_view.Model.SetValue("FMATERIALID", dt.Rows[i]["FMATERIALID"].ToString(), i);
                    string[] str = Regex.Split(dt.Rows[i]["FNUMBER"].ToString(), "_", RegexOptions.IgnoreCase);
                    // string level= dj.Select(x => x.Id = dt.Rows[i]["FAUXPROPID"].ToString()).First();
                    Level level = dj.SingleOrDefault(p => p.Id== dt.Rows[i]["FAUXPROPID"].ToString());
                    if (level != null)
                    {
                        //等级
                        bill_view.Model.SetItemValueByNumber("$$FAUXPROPID__FF100001", level.FAUXPTYNUMBER, i);
                    }
                    bill_view.Model.SetValue("FINVOICE", str[0].ToString(), i);
                    //板号
                    bill_view.Model.SetValue("FBoardNo", str[1].ToString(), i);
                    //箱号
                    bill_view.Model.SetValue("FCartonNo", str[2].ToString(), i);
                    //库存数量
                    bill_view.Model.SetValue("FQTY", dt.Rows[i]["FBASEQTY"].ToString(), i);
                    //入库日期
                    bill_view.Model.SetValue("FInboundDate", dt.Rows[i]["FDATE"].ToString(), i);
                    //下单数量
                    bill_view.Model.SetValue("FOrderQty", qty, i);
                    //批号
                    bill_view.Model.SetValue("FLOT", dt.Rows[i]["FLOT"].ToString(), i);
                    double FPCSCONVERT = 1;
                    if (string.IsNullOrEmpty(dt.Rows[i]["FPCSCONVERT"].ToString()))
                    {
                        FPCSCONVERT = Convert.ToDouble(dt.Rows[i]["FBASEQTY"].ToString());
                    }
                    else
                    {
                        PCSCONVERT = Convert.ToDouble(dt.Rows[i]["FPCSCONVERT"].ToString());
                        FPCSCONVERT = Convert.ToDouble(dt.Rows[i]["FPCSCONVERT"].ToString()) * Convert.ToDouble(dt.Rows[i]["FBASEQTY"].ToString());
                    }
                    FDXQTY = Convert.ToDouble(dt.Rows[i]["FDXQTY"].ToString());
                    //转化pcs数量
                    bill_view.Model.SetValue("FPCSQTY", FPCSCONVERT, i);
                    //保存数据
                    order.FMATERIALID = dt.Rows[i]["FMATERIALID"].ToString();
                    order.FINVOICE = str[0].ToString();
                    order.FBoardNo = str[1].ToString();
                    order.FCartonNo = str[2].ToString();
                    order.FQTY = Convert.ToDouble(dt.Rows[i]["FBASEQTY"].ToString());
                    order.FInboundDate = dt.Rows[i]["FDATE"].ToString();
                    order.FOrderQty = qty;
                    order.FLOT = dt.Rows[i]["FLOT"].ToString();
                    order.FPCSCONVERT = FPCSCONVERT;
                    order.FAUXPROPID = dt.Rows[i]["FAUXPROPID"].ToString();
                    orderPlans.Add(order);
                }
             
                //计算出最优方案 1.计算是刚好等于西单数量的 2.计算出最靠近下单数量的

                // 单箱的pcs
                double SingleFCartonNo = PCSCONVERT * FDXQTY;

                int TotalFCartonNo = Convert.ToInt32(Math.Floor(qty / SingleFCartonNo));

                //剩余数量
                double RemainQty = qty % SingleFCartonNo;
                OrderPlan order2 = new OrderPlan();
                bool Isminimum = false;
                OrderPlan order3 = new OrderPlan();
                double differenceQty = 0;
                double olddifferenceQty = 0;
                double newdifferenceQty2 = 0;
                if (RemainQty != 0)
                {
                                        
                    #region  计算出最靠近剩余数量的数并多于下单数量(最右原则)
                    foreach (var item in orderPlans)
                    {
                        if (differenceQty == 0 && item.FPCSCONVERT - RemainQty>=0)
                        {
                            differenceQty = item.FPCSCONVERT - RemainQty;
                            order2.FMATERIALID = item.FMATERIALID;
                            order2.FINVOICE = item.FINVOICE;
                            order2.FBoardNo = item.FBoardNo;
                            order2.FCartonNo = item.FCartonNo;
                            order2.FQTY = item.FQTY;
                            order2.FInboundDate = item.FInboundDate;
                            order2.FOrderQty = item.FOrderQty;
                            order2.FLOT = item.FLOT;
                            order2.FAUXPROPID = item.FAUXPROPID;
                            order2.FPCSCONVERT = item.FPCSCONVERT;

                        }
                        else
                        {
                            double newdifferenceQty = item.FPCSCONVERT - RemainQty;
                            if (newdifferenceQty > 0 && newdifferenceQty < differenceQty)
                            {

                                differenceQty = newdifferenceQty;
                                order2.FMATERIALID = item.FMATERIALID;
                                order2.FINVOICE = item.FINVOICE;
                                order2.FBoardNo = item.FBoardNo;
                                order2.FCartonNo = item.FCartonNo;
                                order2.FQTY = item.FQTY;
                                order2.FInboundDate = item.FInboundDate;
                                order2.FOrderQty = item.FOrderQty;
                                order2.FLOT = item.FLOT;
                                order2.FAUXPROPID = item.FAUXPROPID;
                                order2.FPCSCONVERT = item.FPCSCONVERT;
                            }
                        }
                    }

                    #endregion

                    #region  计算最靠近的数量并小于下单数量（最左原则）
                    foreach (var item in orderPlans)
                    {

                        newdifferenceQty2 = item.FPCSCONVERT - RemainQty;
                       if (newdifferenceQty2 <=0 )
                       {
                            if (olddifferenceQty != 0)
                            {
                               
                                if(Math.Abs(newdifferenceQty2)< Math.Abs(olddifferenceQty))
                                {
                                    olddifferenceQty = newdifferenceQty2;
                                    order3.FMATERIALID = item.FMATERIALID;
                                    order3.FINVOICE = item.FINVOICE;
                                    order3.FBoardNo = item.FBoardNo;
                                    order3.FCartonNo = item.FCartonNo;
                                    order3.FQTY = item.FQTY;
                                    order3.FInboundDate = item.FInboundDate;
                                    order3.FOrderQty = item.FOrderQty;
                                    order3.FLOT = item.FLOT;
                                    order3.FPCSCONVERT = item.FPCSCONVERT;
                                    order3.FAUXPROPID = item.FAUXPROPID;
                                    Isminimum = true;

                                }

                            }
                            else
                            {
                                olddifferenceQty = newdifferenceQty2;
                                order3.FMATERIALID = item.FMATERIALID;
                                order3.FINVOICE = item.FINVOICE;
                                order3.FBoardNo = item.FBoardNo;
                                order3.FCartonNo = item.FCartonNo;
                                order3.FQTY = item.FQTY;
                                order3.FInboundDate = item.FInboundDate;
                                order3.FOrderQty = item.FOrderQty;
                                order3.FLOT = item.FLOT;
                                order3.FAUXPROPID = item.FAUXPROPID;
                                order3.FPCSCONVERT = item.FPCSCONVERT;
                                Isminimum = true;



                            }

                        }
                       
                        
                    }

                    #endregion

                }
                #region 组装数据
                //当前库存不够发
                if (orderPlans.Count< TotalFCartonNo)
               {

                    returnParam.status = true;
                    returnParam.msg = "当前库存数量不够发货";
                    return returnParam;

               }
                else
                {
                    //排除非整箱
                    List<OrderPlan> orderPlan = orderPlans.FindAll(t=>t.FLOT!= order2.FLOT);
                    List<OrderPlan> orderPlan3 = orderPlans.FindAll(t => t.FLOT != order3.FLOT);
                    int j = 0;
                    #region 最右原则
                    foreach(var item in orderPlan)
                    {
                        if(j== TotalFCartonNo)
                        {
                            break;
                        }
                        else
                        {
                            if(item.FPCSCONVERT== SingleFCartonNo)
                            {
                                bill_view.Model.CreateNewEntryRow("F_PAEZ_Entity");
                                bill_view.Model.SetValue("FMATERIALID2", item.FMATERIALID, j);
                                //等级
                                //bill_view.Model.SetItemValueByID("FAUXPROPID2", item.FAUXPROPID, j);

                                Level level = dj.SingleOrDefault(p => p.Id == item.FAUXPROPID);
                                if (level != null)
                                {
                                    //等级
                                    bill_view.Model.SetItemValueByNumber("$$FAUXPROPID2__FF100001", level.FAUXPTYNUMBER, j);
                                }
                                bill_view.Model.SetValue("FINVOICE2", item.FINVOICE, j);
                                //板号
                                bill_view.Model.SetValue("FBoardNo2", item.FBoardNo, j);
                                //箱号
                                bill_view.Model.SetValue("FCartonNo2", item.FCartonNo, j);
                                //库存数量
                                bill_view.Model.SetValue("FQTY2", item.FQTY, j);
                                //入库日期
                                bill_view.Model.SetValue("FInboundDate2", item.FInboundDate, j);
                                //下单数量
                                bill_view.Model.SetValue("FOrderQty2", item.FOrderQty, j);
                                //批号
                                bill_view.Model.SetValue("FLOT2", item.FLOT, j);
                                //转化pcs数量
                                bill_view.Model.SetValue("FPCSQTY2", item.FPCSCONVERT, j);
                                j++;
                            }
                          
                        }
                       

                    }
                    //加入剩余数量
                    if (RemainQty != 0)
                    {
                        bill_view.Model.CreateNewEntryRow("F_PAEZ_Entity");
                        bill_view.Model.SetValue("FMATERIALID2", order2.FMATERIALID, j);
                        //等级
                        if (!string.IsNullOrEmpty(order2.FAUXPROPID)&& order2.FAUXPROPID!="0")
                        {
                            Level level = dj.SingleOrDefault(p => p.Id == order2.FAUXPROPID);
                            //等级
                            bill_view.Model.SetItemValueByNumber("$$FAUXPROPID2__FF100001", level.FAUXPTYNUMBER, j);
                        }  
                        bill_view.Model.SetValue("FINVOICE2", order2.FINVOICE, j);
                        //板号
                        bill_view.Model.SetValue("FBoardNo2", order2.FBoardNo, j);
                        //箱号
                        bill_view.Model.SetValue("FCartonNo2", order2.FCartonNo, j);
                        //库存数量
                        bill_view.Model.SetValue("FQTY2", order2.FQTY, j);
                        //入库日期
                        bill_view.Model.SetValue("FInboundDate2", order2.FInboundDate, j);
                        //下单数量
                        bill_view.Model.SetValue("FOrderQty2", order2.FOrderQty, j);
                        //批号
                        bill_view.Model.SetValue("FLOT2", order2.FLOT, j);
                        //转化pcs数量
                        bill_view.Model.SetValue("FPCSQTY2", order2.FPCSCONVERT, j);
                    }

                    #endregion

                    #region 最左原则
                    int a = 0;
                    foreach (var item in orderPlan3)
                    {
                        if (a == TotalFCartonNo)
                        {
                            break;
                        }
                        else
                        {
                            if (item.FPCSCONVERT == SingleFCartonNo)
                            {
                                bill_view.Model.CreateNewEntryRow("F_PAEZ_Entity3");
                                bill_view.Model.SetValue("FMATERIALID3", item.FMATERIALID, a);
                                //等级
                                //bill_view.Model.SetItemValueByID("FAUXPROPID3", item.FAUXPROPID, a);
                                Level level = dj.SingleOrDefault(p => p.Id == item.FAUXPROPID);
                                if (level != null)
                                {
                                    //等级
                                    bill_view.Model.SetItemValueByNumber("$$FAUXPROPID3__FF100001", level.FAUXPTYNUMBER, a);
                                }
                                bill_view.Model.SetValue("FINVOICE3", item.FINVOICE, a);
                                //板号
                                bill_view.Model.SetValue("FBoardNo3", item.FBoardNo, a);
                                //箱号
                                bill_view.Model.SetValue("FCartonNo3", item.FCartonNo, a);
                                //库存数量
                                bill_view.Model.SetValue("FQTY3", item.FQTY, a);
                                //入库日期
                                bill_view.Model.SetValue("FInboundDate3", item.FInboundDate, a);
                                //下单数量
                                bill_view.Model.SetValue("FOrderQty3", item.FOrderQty, a);
                                //批号
                                bill_view.Model.SetValue("FLOT3", item.FLOT, a);
                                //转化pcs数量
                                bill_view.Model.SetValue("FPCSQTY3", item.FPCSCONVERT, a);
                                a++;
                            }

                        }


                    }
                    //加入剩余数量
                    if (RemainQty != 0)
                    {
                        bill_view.Model.CreateNewEntryRow("F_PAEZ_Entity3");
                        bill_view.Model.SetValue("FMATERIALID3", order3.FMATERIALID, a);
                        //等级
                        if (!string.IsNullOrEmpty(order3.FAUXPROPID)&& order3.FAUXPROPID!="0")
                        {
                            Level level = dj.SingleOrDefault(p => p.Id == order3.FAUXPROPID);
                            //等级
                            bill_view.Model.SetItemValueByNumber("$$FAUXPROPID3__FF100001", level.FAUXPTYNUMBER, a);
                        }
                        bill_view.Model.SetValue("FINVOICE3", order3.FINVOICE, a);
                        //板号
                        bill_view.Model.SetValue("FBoardNo3", order3.FBoardNo, a);
                        //箱号
                        bill_view.Model.SetValue("FCartonNo3", order3.FCartonNo, a);
                        //库存数量
                        bill_view.Model.SetValue("FQTY3", order3.FQTY, a);
                        //入库日期
                        bill_view.Model.SetValue("FInboundDate3", order3.FInboundDate, a);
                        //下单数量
                        bill_view.Model.SetValue("FOrderQty3", order3.FOrderQty, a);
                        //批号
                        bill_view.Model.SetValue("FLOT3", order3.FLOT, a);
                        //转化pcs数量
                        bill_view.Model.SetValue("FPCSQTY3", order3.FPCSCONVERT, a);
                    }

                    #endregion
                }
                #endregion 
                string result = "";

               IOperationResult save_result = bill_view.Model.Save();
               if (save_result.IsSuccess)
               {
                    string fid = string.Empty;
                    OperateResultCollection Collection = save_result.OperateResult;
                    foreach(var item in Collection)
                    {

                        fid = item.PKValue.ToString();
                    }
                    returnParam.FBUSINESSCODE = fid;
                    returnParam.FBIZFORMID = "PAEZ_OrderPlan";
                    returnParam.msg = "方案生成功";
                    returnParam.status = true;
                    return returnParam;
                }
               else
               {
                   for (int mf = 0; mf < save_result.ValidationErrors.Count; mf++)
                   {
                       result += "\r\n" + save_result.ValidationErrors[mf].Message;
                   }
               }
            

                #endregion


            }
            catch (Exception ex)
            {

                returnParam.msg = ex.ToString();
                returnParam.status = false;
                returnParam.msg = "方案生成失败";
                return returnParam;
            }
            return returnParam;
        }

    }
}
