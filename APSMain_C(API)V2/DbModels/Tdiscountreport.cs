using System;
using System.Collections.Generic;

namespace APSMain.DbModels;

/// <summary>
/// 할인권 매출
/// </summary>
public partial class Tdiscountreport
{
    /// <summary>
    /// PK
    /// </summary>
    public int Xindex { get; set; }

    /// <summary>
    /// 매장코드 (tdiscountdept.deptcode처럼 사용)
    /// </summary>
    public int Deptcode { get; set; }

    /// <summary>
    /// 구매자/계정 id (tdiscountaccount.id처럼 사용)
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// 할인종류 (tdiscounttable.salecode처럼 사용)
    /// </summary>
    public int Salecode { get; set; }

    /// <summary>
    /// 매수(장)
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// 구매일자
    /// </summary>
    public DateTime Purchasedate { get; set; }

    /// <summary>
    /// 구매금액(원) = quantity * saleprice 스냅샷
    /// </summary>
    public int Amount { get; set; }
}
