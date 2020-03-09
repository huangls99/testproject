using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaFeeg.K3Cloud.Developments
{
    /// <summary>
    /// 由子窗体返回给父窗体的数据对象
    /// </summary>
    public class ReturnInfo
    {
        public string Fnumber { get; set; }
        public int Qty { get; set; }
        
       public string FGG { get; set; }

    }
    /// <summary>
    /// 返回的参数
    /// </summary>
    public class ReturnParam
    {

        public string FBIZFORMID { set; get; } //单据标识

        public string FBUSINESSCODE { set; get; } //单据内码

        public string msg { set; get; } //信息

        public bool status { set; get; } //返回成功或者失败



    }
    /// <summary>
    /// 订单信息
    /// </summary>
    public class OrderPlan
    {
        public string FMATERIALID { set; get; }

        public string FINVOICE { set; get; }

        public string FBoardNo { set; get; }
        public string FCartonNo { set; get; }
        public string FInboundDate { set; get; }

        public double FQTY { set; get; }

        public double FOrderQty { set; get; }

        public string FLOT { set; get; }

        public double FPCSCONVERT { set; get; }
        public string FAUXPROPID { set; get; }

    }
    /// <summary>
    /// 等级信息
    /// </summary>
    public class Level
    {
        public string Id { set; get; }

        public string FAUXPTYNUMBER { set; get; }

    }
}
