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
using System.Data;

namespace MaFeeg.K3Cloud.Developments
{
    public class ICStockBill : AbstractBillPlugIn
    {
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);
            if (e.BarItemKey == "tb_ImportFileUpdateEdit")
            {
                this.View.ShowMessage("测试成功！",MessageBoxType.Notice);
            }
        }
    }
}
