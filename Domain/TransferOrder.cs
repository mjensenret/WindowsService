//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Domain
{
    using System;
    using System.Collections.Generic;
    
    public partial class TransferOrder
    {
        public long transferOrderId { get; set; }
        public long transferTypeId { get; set; }
        public long siteId { get; set; }
        public Nullable<long> transferSubTypeId { get; set; }
        public long customerId { get; set; }
        public long productId { get; set; }
        public Nullable<long> shipperId { get; set; }
        public Nullable<long> loaderId { get; set; }
        public Nullable<long> consigneeAreaId { get; set; }
        public Nullable<long> transferOrderLoaderId { get; set; }
        public string shipperOrderNumber { get; set; }
        public Nullable<System.DateTime> scheduledDate { get; set; }
        public Nullable<System.DateTime> orderedDate { get; set; }
        public Nullable<System.DateTime> deliveryDate { get; set; }
        public Nullable<decimal> orderWeight { get; set; }
        public Nullable<long> weightUnitOfMeasureId { get; set; }
        public Nullable<decimal> orderVolume { get; set; }
        public Nullable<long> volumeUnitOfMeasureId { get; set; }
        public Nullable<long> consigneeId { get; set; }
        public string consigneeOrderNumber { get; set; }
        public Nullable<long> carrierId { get; set; }
        public string freightCharge { get; set; }
        public string poNumber { get; set; }
        public string workOrderComment { get; set; }
        public string bolComment { get; set; }
        public Nullable<System.DateTime> scaleInDate { get; set; }
        public Nullable<decimal> scaleInWeight { get; set; }
        public Nullable<System.DateTime> scaleOutDate { get; set; }
        public Nullable<long> departureEquipmentTypeId { get; set; }
        public string departureEquipmentName { get; set; }
        public string departureSealNumbers { get; set; }
        public Nullable<System.DateTime> loadStartDate { get; set; }
        public Nullable<System.DateTime> loadEndDate { get; set; }
        public Nullable<decimal> strength { get; set; }
        public bool isCompleted { get; set; }
        public bool isVoided { get; set; }
        public System.DateTime inputDate { get; set; }
        public string inputBy { get; set; }
        public System.DateTime modifiedDate { get; set; }
        public string modifiedBy { get; set; }
        public Nullable<long> billToId { get; set; }
        public Nullable<bool> isValidated { get; set; }
        public Nullable<long> productWeightUnitOfMeasureId { get; set; }
        public Nullable<System.DateTime> interfaceDate { get; set; }
        public string status { get; set; }
        public Nullable<long> departureEquipmentId { get; set; }
        public Nullable<long> productAliasId { get; set; }
        public string processBy { get; set; }
        public string DriverName { get; set; }
        public string TruckLicensePlateNumber { get; set; }
        public string RackIdNumber { get; set; }
    }
}
