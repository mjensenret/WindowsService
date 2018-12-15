using Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logging;
using Domain.Service;
using Domain.Models;

namespace Domain.Repositories
{
    public class TransferOrderRepository
    {
        TestTransferContext db = new TestTransferContext();
        TransloadWebService svc = new TransloadWebService();
        

        public TransferOrderRepository()
        {
            ServiceLog.Init(null, null, null, @"C:\Logs\ScaleIntegration\RepositoryLog.log", @"C:\Logs\ScaleIntegration\RepositoryErrorLog.log", true, 10);
        }

        public bool TransferOrderValid(int transferOrderId)
        {
            try
            {
                var model = svc.getTransferOrder(transferOrderId);

                if (svc.checkAuditNumber(transferOrderId))
                {
                    ServiceLog.Default.Trace("Transfer order Id: {0} was found and in scheduled status.", transferOrderId.ToString());
                    return true;
                }
                //var query = from t in db.TransferOrders
                //            where t.transferOrderId == transferOrderId
                //            where t.status == "SCHEDULED"
                //            select t;
                //if (query.Count() >= 1)
                //{
                //    ServiceLog.Default.Trace("Transfer order Id: {0} was found and in scheduled status.", transferOrderId.ToString());
                //    return true;
                //}
                
            }
            catch (Exception e)
            {
                ServiceLog.Default.Log("Error parsing the transfer order Id: {0}", transferOrderId.ToString());
                ServiceLog.Default.LogError(e);
            }


            ServiceLog.Default.Trace("Transfer order Id: {0} was either not in scheduled status, or not found.", transferOrderId.ToString());

            return false;

        }

        public bool UpdateInboundScaleData(int transferOrderId, int sequenceNumber, int driverId, int truckNumber, int trailerNumber, decimal weight, DateTime scaleInDate)
        {
            ServiceLog.Default.Trace(@"Attempting to update Inbound data:
                                        Transfer Order: {0}
                                        Sequence Number: {1}
                                        Driver Id: {2}
                                        Truck: {3}
                                        Trailer: {4}
                                        Weight: {5}
                                        Scale Date: {6}",
                                        transferOrderId.ToString(),
                                        sequenceNumber.ToString(),
                                        driverId.ToString(),
                                        truckNumber.ToString(),
                                        trailerNumber.ToString(),
                                        weight.ToString(),
                                        scaleInDate.ToString()
                                        );

            TransferOrderModel transferOrder = svc.getTransferOrder(transferOrderId);

            if (transferOrder != null)
            {
                if (svc.UpdateTransferOrder(transferOrder, "Inbound", transferOrderId, sequenceNumber, scaleInDate, null, trailerNumber, null, weight))
                {
                    ServiceLog.Default.Trace("Inbound scale message for id:{0} was processed successfully.", transferOrderId.ToString());
                    return true;
                }
                else
                {
                    ServiceLog.Default.Trace("There was an error updating the inbound scale message, or the record already has matching values.");
                    return false;
                }
                    

 
            }
            else
            {
                ServiceLog.Default.Trace("Transfer order object was null for id {0}", transferOrderId.ToString());
                return false;
            }
            
        }

        public bool UpdateOutboundScaleData(int transferOrderId, int sequenceNumber, int loaderId, decimal weight, DateTime scaleOutDate)
        {
            ServiceLog.Default.Trace(@"Attempting to update outbound data:
                                        Transfer Order: {0}
                                        Sequence Number: {1}
                                        Loader Id: {2}
                                        Weight: {3}
                                        Scale Out Date: {4}",
                            transferOrderId.ToString(),
                            sequenceNumber.ToString(),
                            loaderId.ToString(),
                            weight.ToString(),
                            scaleOutDate.ToString()
                            );

            TransferOrderModel transferOrder = svc.getTransferOrder(transferOrderId);

            if (transferOrder != null)
            {
                if (svc.UpdateTransferOrder(transferOrder, "Outbound", transferOrderId, sequenceNumber, null, scaleOutDate, null, loaderId, weight))
                {
                    ServiceLog.Default.Trace("Outbound scale message for transfer order id {0} sequence {1} was processed successfully.", transferOrderId.ToString(), sequenceNumber.ToString());
                    return true;
                }
                else
                {
                    ServiceLog.Default.Trace("There was an error updating the outbound scale message, or the record already has matching values.");
                    return false;
                }

            }
            else
            {
                ServiceLog.Default.Trace("Transfer order object was null for id {0}", transferOrderId.ToString());
                return false;
            }

        }

    }
}
