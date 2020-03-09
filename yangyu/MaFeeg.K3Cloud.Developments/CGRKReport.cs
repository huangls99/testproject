using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Core.Report;
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
    [Description("采购入库单报表插件")]
    [Kingdee.BOS.Util.HotUpdate]
   public class CGRKReport : SysReportBaseService
   {
        /// <summary>
        /// 初始化
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            // 简单账表类型：普通、树形、分页
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;// 报表名称
            this.ReportProperty.ReportName = new LocaleValue("采购入库单报表", base.Context.UserLocale.LCID);
            this.IsCreateTempTableByPlugin = true;
            this.ReportProperty.IsGroupSummary = true;
            this.ReportProperty.IsUIDesignerColumns = false;
            // 单据主键：两行FID相同，则为同一单的两条分录，单据编号可以不重复显示
             this.ReportProperty.PrimaryKeyFieldName = "FNUMBER";
            // 报表主键字段名：默认为FIDENTITYID，可以修改
            //this.ReportProperty.IdentityFieldName = "FIDENTITYID";
           
        }
        /// <summary>
        /// 获取表格
        /// </summary>
        /// <returns></returns>
        public override string GetTableName()
        {
            var result = base.GetTableName();
            return result;
        }

        /// <summary>
        /// 动态添加列
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public override Kingdee.BOS.Core.Report.ReportHeader GetReportHeaders(IRptParams filter)
        {
            //FMATERIALID ,sum(FREALQTY) as FREALQTY,FSTOCKID,FUNITID 
            ReportHeader header = new ReportHeader();
            //物料编码
            header.AddChild("FNUMBER", new LocaleValue("物料编码"), SqlStorageType.Sqlnvarchar);
            var itemName = header.AddChild("fFNAME", new LocaleValue("单位名称"), SqlStorageType.Sqlvarchar);
            itemName.Width = 100;
            header.AddChild("eFNAME", new LocaleValue("仓库名称"), SqlStorageType.Sqlvarchar);
            header.AddChild("FREALQTY", new LocaleValue("总数"), SqlStorageType.SqlDecimal);
            header.AddChild("ApprovedQty", new LocaleValue("已审核数量"), SqlStorageType.SqlDecimal);
            header.AddChild("NotApprovedQty", new LocaleValue("未审核数量"), SqlStorageType.SqlDecimal);

            return header;
        }


        /// <summary>
        /// 向报表临时表插入报表数据, 必须是往tableName查数据
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="tableName"></param>
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            //该行代码必须要，否则数据显示不出来

            base.BuilderReportSqlAndTempTable(filter, tableName);
            DynamicObject dyFilter = filter.FilterParameter.CustomFilter; //获取过滤框快捷框字段信息
            int orgId = (int)this.Context.CurrentOrganizationInfo.ID;
            string sql2 = "";
            //组织
            string FOrganization = dyFilter["FOrganization_Id"].ToString();
            if (string.IsNullOrEmpty(FOrganization))
            {
                sql2 = " b.FPURCHASEORGID = '"+ orgId + "' ";
            }
            else
            {
                sql2 = " b.FPURCHASEORGID = '" + FOrganization + "' ";
            }
            //仓库
            string FWarehouse = dyFilter["FWarehouse_Id"].ToString();
            if (FWarehouse!="0")
            {
                sql2 += " and a.FSTOCKID = '" + FWarehouse + "' ";
            }
            //物料
            string FMATERIALID = dyFilter["FMATERIALID_Id"].ToString();
            if (FMATERIALID!="0"&&!string.IsNullOrEmpty(FMATERIALID))
            {
                sql2 += " and a.FMATERIALID = '" + FMATERIALID + "' ";
            }
            //开始日期
            string FStartDate = dyFilter["FStartDate"].ToString();
            if (!string.IsNullOrEmpty(FStartDate))
            {
                sql2 += " and b.FDATE>= '" + FStartDate + "' ";
            }
            //开始日期
            string FEndDate = dyFilter["FEndDate"].ToString();
            if (!string.IsNullOrEmpty(FEndDate))
            {
                sql2 += " and b.FDATE<= '" + FEndDate + "' ";
            }
            string seqFld = string.Format(base.KSQL_SEQ, "a.FNUMBER");//用于生成自增列的
            string  sql3= "  group by a.FNUMBER,a.eFNAME ,a.fFNAME";
            string sql =string.Format(@" select  a.FNUMBER,sum(a.FREALQTY) as FREALQTY ,a.eFNAME as eFNAME ,a.fFNAME,isnull(sum(b.FREALQTY),0) as ApprovedQty ,(sum(a.FREALQTY)-isnull(sum(b.FREALQTY),0)) as NotApprovedQty ,{0} into {1}
                                          from (select c.FNUMBER  ,sum(FREALQTY) as FREALQTY ,e.FNAME as eFNAME ,f.FNAME as fFNAME from T_STK_INSTOCKENTRY a 
                                         left join T_BD_MATERIAL c on a.FMATERIALID=c.FMATERIALID
                                         left join T_BD_STOCK_L e on e.FSTOCKID=a.FSTOCKID
                                         left join T_BD_UNIT_L f on f.FUNITID=a.FUNITID
                                         left  join  t_STK_InStock b on b.FID=a.FID  where {2}
                                         group by c.FNUMBER,e.FNAME,f.FNAME)  a  FULL JOIN ( select c.FNUMBER  ,sum(FREALQTY) as FREALQTY ,e.FNAME as eFNAME ,f.FNAME as fFNAME from T_STK_INSTOCKENTRY a 
                                         left join T_BD_MATERIAL c on a.FMATERIALID=c.FMATERIALID
                                         left join T_BD_STOCK_L e on e.FSTOCKID=a.FSTOCKID
                                         left join T_BD_UNIT_L f on f.FUNITID=a.FUNITID
                                         left  join  t_STK_InStock b on b.FID=a.FID where   b.FDOCUMENTSTATUS='C' and  {3}
                                         group by c.FNUMBER,e.FNAME,f.FNAME) b on a.FNUMBER=b.FNUMBER {4}", seqFld, tableName,sql2, sql2, sql3);

            //sql = "select FNUMBER  ,sum(FREALQTY) as FREALQTY,e.FNAME as eFNAME ,f.FNAME as fFNAME    ," + seqFld + " into " + tableName + " from T_STK_INSTOCKENTRY a " +
            //     " left join T_BD_MATERIAL c on a.FMATERIALID=c.FMATERIALID" +
            //     " left join T_BD_STOCK_L e on e.FSTOCKID=a.FSTOCKID" +
            //     " left join T_BD_UNIT_L f on f.FUNITID=a.FUNITID" +
            //     " left join  t_STK_InStock b on b.FID = a.FID where";
            //执行
            DBServiceHelper.Execute(this.Context,sql);



        }
        /// <summary>
        /// 设置报表合计列
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public override List<SummaryField> GetSummaryColumnInfo(IRptParams filter)
        {
            var result = base.GetSummaryColumnInfo(filter);
            result.Add(new SummaryField("FREALQTY", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            return result;
        }
    }
}
