using Kingdee.BOS.Core.List.PlugIn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaFeeg.K3Cloud.Developments
{
    public class Class1 : AbstractListPlugIn
    {
    }
}


/*
五、字段说明:
单据编号:FBillNo 
货主:FOWNERID 
货主类型:FOWNERTYPEID 
材料成本:FMaterialCosts 
加工费:FProcessFee 
收货库存更新标志:FReceiveStockFlag 
关联应付数量（计价基本）:FBaseAPJoinQty 
已钩稽数量:FJOINEDQTY 
已开票数量:FINVOICEDQTY 
保管者类型:FKeeperTypeId 
源单编号:FSRCBillNo 
源单类型:FSRCBILLTYPEID 
保质期:FExpPeriod 
保质期单位:FEXPUnit 
仓位:FStockLocId 
保管者:FKeeperID 
收货库存状态:FReceiveStockStatus 
未钩稽数量:FUNJOINQTY 
未钩稽金额:FUNJOINAMOUNT 
折扣额:FDiscount 
金额（本位币）:FAmount_LC 
金额:FAmount 
价格下限:FDownPrice 
价格上限:FUpPrice 
系统定价:FSysPrice 
已钩稽金额:FJOINEDAMOUNT 
单价:FPrice 
收货货主:FReceiveOwnerId 
收货货主类型:FReceiveOwnerTypeId 
批号:FLot 
在架寿命期:FShelfLife 
行钩稽状态:FJOINSTATUS 
完全钩稽:FFULLYJOINED 
税组合:FTaxCombination 
退料关联数量(库存基本):FBaseReturnJoinQty 
订单单号:FPOOrderNo 
关联数量(基本单位):FBaseJoinQty 
成本价:FCostPrice 
含税单价:FTaxPrice 
BOM版本:FBOMId 
库存状态:FStockStatusId 
免费:FIsFree 
有效期至:FExpiryDate 
税率(%):FEntryTaxRate 
源单行内码:FSRCRowId 
实收数量:FRealQty 
应收数量:FMustQty 
需求跟踪号:FReqTraceNo 
合同单号:FContractlNo 
规格型号:FUOM 
物料类别:FMaterialType 
数量（库存辅单位）:FAuxUnitQty 
辅助属性:FAuxPropId 
税额:FEntryTaxAmount 
价格系数:FPriceCoefficient 
计价单位:FPriceUnitID  (必填项)
库存辅单位:FAuxUnitID 
库存基本数量:FBaseUnitQty 
基本单位:FBaseUnitID 
库存单位:FUnitID  (必填项)
基本单位单价:FBaseUnitPrice 
折扣率(%):FDiscountRate 
入库库存更新标志:FStockFlag 
总成本(本位币):FCostAmount_LC 
税额(本位币):FTaxAmount_LC 
价税合计:FAllAmount 
总成本:FEntryCostAmount 
净价:FTaxNetPrice 
计价数量:FPriceUnitQty 
价税合计(本位币):FAllAmount_LC 
物料名称:FMaterialName 
退料关联数量:FReturnJoinQty 
收货批号:FReceiveLot 
单箱数:F_PAEZ_Decimal 
箱号:F_PAEZ_Text2 
板号:F_PAEZ_Text1 
材料成本(本位币):FMaterialCosts_LC 
加工费(本位币):FProcessFee_LC 
第三方单据分录ID:FTHIRDENTRYID 
净重:F_PAEZ_Decimal1 
父行标识:FParentRowId 
父项产品:FParentMatId 
产品类型:FRowType 
收料辅序子单据体内码:FRECSUBENTRYID 
拆单前原计价数量:FBeforeDisPriceQty 
拆单数量（计价）:FDisPriceQty 
应付关闭日期:FPAYABLECLOSEDATE 
行标识:FRowId 
应付关闭状态:FPayableCloseStatus 
毛重:F_PAEZ_Decimal2 
单箱毛重:F_PAEZ_Decimal4 
序列号:FSerialNo 
买方代扣代缴:FBuyerWithholding 
卖方代扣代缴:FSellerWithholding 
增值税:FVAT 
计入成本金额:FTaxCostAmount 
计入成本比例%:FCostPercent 
单箱净重:F_PAEZ_Decimal3 
税额:FTaxAmount 
税率名称:FTaxRateId 
费用名称:FCostName 
费用代码:FCostId 
备注:FCostNOTE 
金额:FCostAmount 
箱规则:F_PAEZ_Text3 
税率%:FTaxRate 
业务流程:FBFLowId 
关联应付金额:FAPJoinAmount 
分录价目表:FPriceListEntry 
开票结束状态:FInvoicedStatus 
收料更新库存:FIsReceiveUpdateStock 
来料检验:FCheckInComing 
样本破坏数量（计价基本）:FSampleDamageBaseQty 
样本破坏数量(计价单位):FSampleDamageQty 
辅助单位退料关联数量:FSECRETURNJOINQTY 
已开票关联数量:FInvoicedJoinQty 
序列号单位数量:FSNQty 
是否赠品:FGiveAway 
项目编号:FProjectNo 
计划跟踪号:FMtoNo 
收货辅助属性:FReceiveAuxPropId 
收货仓位:FReceiveStockLocId 
收货仓库:FReceiveStockID 
序列号单位:FSNUnitID 
未关联应付数量（计价单位）:FAPNotJoinQty 
辅单位:FExtAuxUnitId 
收货计划跟踪号:FReceiveMtoNo 
采购订单分录内码:FPOORDERENTRYID 
成本价(本位币):FCOSTPRICE_LC 
关联应付数量（库存基本):FStockBaseAPJoinQty 
退料关联数量(采购基本):FRETURNSTOCKJNBASEQTY 
携带的主业务单位:FSRCBIZUNITID 
库存基本分母:FStockBaseDen 
实收数量(辅单位):FExtAuxUnitQty 
采购基本分子:FPURBASENUM 
采购基本数量:FRemainInStockBaseQty 
采购数量:FRemainInStockQty 
采购单位:FRemainInStockUnitId  (必填项)
定价单位:FSetPriceUnitID 
计价基本数量:FPriceBaseQty 
入库类型:FWWInType 
立账关闭:FBILLINGCLOSE 
序列号:FSerialId 
净重:FNetWeight 
供应商批号:FSupplierLot 
结算方:FSettleId 
供货方:FSupplyId 
供货方联系人(旧):FSupplyContact 
收款方:FChargeId 
采购员:FPurchaserId 
采购组:FPurchaserGroupId 
采购部门:FPurchaseDeptId 
业务类型:FBusinessType 
创建日期:FCreateDate 
创建人:FCreatorId 
仓管员:FStockerId 
收料部门:FStockDeptId 
库存组:FStockerGroupId 
供应商:FSupplierId  (必填项)
最后修改人:FModifierId 
采购组织:FPurchaseOrgId  (必填项)
货主:FOwnerIdHead  (必填项)
货主类型:FOwnerTypeIdHead  (必填项)
单据类型:FBillTypeID  (必填项)
入库日期:FDate  (必填项)
收料组织:FStockOrgId  (必填项)
单据状态:FDocumentStatus 
需求组织:FDemandOrgId 
最后修改日期:FModifyDate 
作废日期:FCancelDate 
作废状态:FCancelStatus 
审核日期:FApproveDate 
作废人:FCancellerId 
审核人:FApproverId 
提货单号:FTakeDeliveryBill 
送货单号:FDeliveryBill 
毛重:FGrossWeight 
价税合计(本位币):FBillAllAmount_LC 
总成本(本位币):FBilCostAmount_LC 
税额(本位币):FBillTaxAmount_LC 
本位币:FLocalCurrId 
汇率:FExchangeRate 
付款条件:FPayConditionId 
折扣表:FDiscountListId 
汇率类型:FExchangeTypeId 
价税合计:FBillAllAmount 
总成本:FBillCostAmount 
税额:FBillTaxAmount 
整单费用:FBillCost 
结算币别:FSettleCurrId  (必填项)
结算组织:FSettleOrgId  (必填项)
结算方式:FSettleTypeId 
付款组织:FPayOrgId 
定价时点:FPriceTimePoint  (必填项)
金额(本位币):FBillAmount_LC 
备注:FNote 
生产日期:FProduceDate 
仓库:FStockId 
物料编码:FMaterialId  (必填项)
第三方单据编号:FTHIRDBILLNO 
第三方单据ID:FTHIRDBILLID 
价目表:FPriceListId 
第三方来源:FTHIRDSRCTYPE 
价外税:FISPRICEEXCLUDETAX 
跨组织结算生成:FISGENFORIOS 
结算组织供应商:FSettleSupplierID 
货主客户:FOwnerCustomerID 
含税:FIsIncludedTax 
金额:FBillAmount 
先到票后入库:FISINVOICEARLIER 
Invoice No:F_PAEZ_Text 
对应组织:FCorrespondOrgId 
跨组织业务类型:FTransferBizType 
应付状态:FAPSTATUS 
供货方地址:FSupplyAddress 
供货方联系人:FProviderContactID 
确认状态:FConfirmStatus 
确认日期:FConfirmDate 
确认人:FConfirmerId 
拆单新单标识:FDisassemblyFlag 
创建日期偏移量:FCDateOffsetValue 
创建日期偏移单位:FCDateOffsetUnit 
序列号上传:FScanBox 
组织间结算跨法人标识:FIsInterLegalPerson 
备注:FSerialNote
 
 
 
 #region 修改单据数据
                            //获取采购入库的元数据
                            FormMetadata meta = MetaDataServiceHelper.Load(this.Context, "STK_InStock") as FormMetadata;
                            BusinessInfo info = meta.BusinessInfo;
                            //Load需要修改单据的数据包:加载内码为100001的销售订单数据
                            DynamicObject toModifyObj = Kingdee.BOS.ServiceHelper.BusinessDataServiceHelper.LoadSingle(this.Context, BillID, info.GetDynamicObjectType());

                            if (toModifyObj != null)
                            {
                                //修改单据头字段
                                //修改基础资料字段，用内码20002赋值
                                //(info.GetField("FCustId") as BaseDataField).RefIDDynamicProperty.SetValue(toModifyObj, 20002);
                                //修改一般字段的值
                                //info.GetField("FNote").DynamicProperty.SetValue(toModifyObj, "测试一下的备注");

                                //修改分录，比如修改订单明细
                                //先获取分录数据集合
                                DynamicObjectCollection entryObjs = toModifyObj["InStockEntry"] as DynamicObjectCollection;
                                //循环修改所有分录行或者找到你需要修改的分录行，然后进行字段赋值修改
                                foreach (DynamicObject entryObj in entryObjs)
                                {
                                    //字段赋值方式可参考单据头的，赋值方式是类似的
                                    //数量
                                    info.GetField("FTaxPrice").DynamicProperty.SetValue(entryObj, 89);
                                    info.GetField("FTaxPrice").RefIDDynamicProperty.SetValue(entryObj, 89);
                                }
                                //然后调用保存服务接口,得到保存结果result
                                IOperationResult save_result = Kingdee.BOS.ServiceHelper.BusinessDataServiceHelper.Save(this.Context, info, new DynamicObject[] { toModifyObj }, null, "Save");
                                if (save_result.IsSuccess)
                                {
                                    //成功
                                    result += "\r\n引入成功！\r\n________________________________________________________________________\r\n";
                                    continue;
                                }
                                else
                                {
                                    //失败
                                    for (int mf = 0; mf < save_result.ValidationErrors.Count; mf++)
                                    {
                                        result += "\r\n" + save_result.ValidationErrors[mf].Message;
                                    }
                                    result += "\r\n________________________________________________________________________\r\n";
                                    continue;
                                }
                            }
                            #endregion
*/
