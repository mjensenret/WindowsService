using Domain.Models;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Deserializers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;
using Domain;
using Logging;

namespace Domain.Service
{
    class TransloadWebService
    {
        string baseUrl = "http://transload-test2.savageservices.com:9595/api/scale/";
        string user = "api";
        string pass = "api";

        public TransloadWebService() { }

        public RestClient getUrl(int transferOrderId)
        {
            try
            {
                var client = new RestClient(baseUrl + transferOrderId)
                {
                    Authenticator = new HttpBasicAuthenticator(user, pass)
                };
                return client;
            }
            catch (Exception e)
            {
                ServiceLog.Default.Trace("Unable to create client connection.");
                return null;
            }

        }

        public bool checkAuditNumber(int transferOrderId)
        {
            var client = getUrl(transferOrderId);
            var request = new RestRequest(Method.GET);
            request.RequestFormat = DataFormat.Json;

            var response = client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                ServiceLog.Default.Trace("Audit number {0} is available.", transferOrderId.ToString());
                return true;

            }
            else
            {
                ServiceLog.Default.Trace("Audit number {0} is invalid.", transferOrderId.ToString());
                return false;
            }
        }

        public TransferOrderModel getTransferOrder(int Id)
        {
            var client = getUrl(Id);
            var request = new RestRequest(Method.GET);
            request.RequestFormat = DataFormat.Json;

            try
            {
                var response = client.Execute(request);
                
                var model = JsonConvert.DeserializeObject<TransferOrderModel>(response.Content);
                
                return model;
            }
            catch (Exception e)
            {
                ServiceLog.Default.Trace("There was an error getting the model for transfer order {0}", Id.ToString());
                ServiceLog.Default.LogError(e);
                return null;
            }

        }

        public bool UpdateTransferOrder(TransferOrderModel order, string function, int transferOrderId, int sequenceNumber, DateTime? loadStartDate, DateTime? loadEndDate, int? equipmentId, int? loaderId, decimal weight)
        {
            var client = getUrl(transferOrderId);
            var request = new RestRequest(Method.PATCH);
            request.AddHeader("Content-Type", "application/json-patch+json");
            request.RequestFormat = DataFormat.Json;

            TransferOrderModel updTO = order;
            bool splitLoad = updTO.isSplit;
            updTO.transferOrderArrivals = updTO.transferOrderArrivals.Where(x => x.sequenceNumber == sequenceNumber).ToList();

            bool previouslyCompleted = false;

            if (function == "Inbound")
            {
                if (updTO.transferOrderArrivals.Single().scaleInWeight == null || updTO.transferOrderArrivals.Single().scaleInWeight != weight)
                {
                    var updLoadStartDate = JsonConvert.ToString(Convert.ToDateTime(loadStartDate).ToUniversalTime());
                    updLoadStartDate = updLoadStartDate.Replace("\"", "");

                    updTO.transferOrderArrivals.Single().scaleInWeight = weight;
                    updTO.transferOrderArrivals.Single().scaleInDate = updLoadStartDate;

                    if (splitLoad)
                    {
                        if (sequenceNumber == 1)
                        {
                            updTO.departureEquipmentName = equipmentId.ToString();
                            updTO.loadStartDate = updLoadStartDate;
                        }
                    }
                    else
                    {
                        updTO.departureEquipmentName = equipmentId.ToString();
                        updTO.loadStartDate = updLoadStartDate;
                    }
                }
                else
                {
                    ServiceLog.Default.Trace("Record already has a scale in weight that matches for id {0}, sequence {1}", updTO.transferOrderId.ToString(), updTO.transferOrderArrivals.Single().sequenceNumber.ToString());
                    previouslyCompleted = true;
                }
            }
            else
            {
                var updLoadEndDate = JsonConvert.ToString(Convert.ToDateTime(loadEndDate).ToUniversalTime());
                updLoadEndDate = updLoadEndDate.Replace("\"", "");

                if (updTO.transferOrderArrivals.Single().scaleOutWeight == null || updTO.transferOrderArrivals.Single().scaleOutWeight != weight)
                {
                    updTO.transferOrderArrivals.Single().scaleOutWeight = weight;
                    updTO.transferOrderArrivals.Single().netTransferWeight = Math.Abs(Convert.ToDecimal(updTO.transferOrderArrivals.Single().scaleInWeight) - weight);
                    updTO.transferOrderArrivals.Single().scaleOutDate = updLoadEndDate;
                    updTO.transferOrderLoaderId = loaderId;

                    if (splitLoad)
                    {
                        if (sequenceNumber != 1)
                        {
                            updTO.loadEndDate = updLoadEndDate;

                        }
                    }
                    else
                    {
                        updTO.loadEndDate = updLoadEndDate;
                    }
                }
                else
                {
                    ServiceLog.Default.Trace("Record already has a scale out weight that matches for id {0}, sequence {1}", updTO.transferOrderId.ToString(), updTO.transferOrderArrivals.Single().sequenceNumber.ToString());
                    previouslyCompleted = true;
                }

            }

            if (!previouslyCompleted)
            {
                try
                {
                    string json = JsonConvert.SerializeObject(updTO);
                    request.AddParameter("application/json-patch+json", json, ParameterType.RequestBody);

                    var response = client.Execute(request);

                    if (response.IsSuccessful)
                    {
                        ServiceLog.Default.Trace("Transfer order Id {0}, sequence {1} was successfully updated.", transferOrderId.ToString(), sequenceNumber.ToString());
                        return true;
                    }
                }
                catch (Exception e)
                {
                    ServiceLog.Default.Trace("Exception has occurred: ", e.Message);
                    return false;
                }
            }

            return false;
        }

        public bool UpdateInboundScaleData(int transferOrderId, int sequenceNumber, int driverId, int truckNumber, int trailerNumber, decimal weight, DateTime scaleInDate)
        {
            TransferOrderModel transferOrder = getTransferOrder(transferOrderId);

            return false;
        }


    }
}
