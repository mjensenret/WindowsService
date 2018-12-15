using Domain.Repositories;
using Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpServerLib.IO;

namespace WindowsTestService
{
    public class ScaleProtocolHandler : ProtocolHandlerBase
    {
        private static TransferOrderRepository repo = new TransferOrderRepository();

        public ScaleProtocolHandler()
        {
            
            //AddCommandHandler(GetSampleStoreDataCommandPattern(), HandleSampleStoreDataCommandMessage);
            AddCommandHandler(GetQueryDataCommandPattern(), HandleQueryDataCommandMessage);
            AddCommandHandler(GetInboundDataCommandPattern(), HandleInboundCommandMessage);
            AddCommandHandler(GetOutboundDataCommandPattern(), HandleOutboundCommandMessage);
        }

        private string GetSampleStoreDataCommandPattern()
        {
            // SAMPLE_COMMAND|DATA1|DATA2|DATA3|DATA4|
            // DATA1 = string, up to 50 characters, required
            // DATA2 = int, between 1-10
            // DATA3 = weight value
            // DATA4 = Date Time in MMddyy HHmmss format

            var pattern = @"^SAMPLE_COMMAND" + GetFieldDelimiterPattern();
            pattern += GetRequiredStringPattern(50) + GetFieldDelimiterPattern();
            pattern += GetUnsignedIntegerPattern(1, 10) + GetFieldDelimiterPattern();
            pattern += GetWeightPattern() + GetFieldDelimiterPattern();
            pattern += GetDateTimePattern() + GetFieldDelimiterPattern();

            return pattern;
        }

        private string GetUnsignedIntegerPattern(int minPlaces, int maxPlaces)
        {
            return @"[0-9]{" + minPlaces + "," + maxPlaces + "}";
        }

        private string HandleSampleStoreDataCommandMessage(string message)
        {
            try
            {
                var fields = message.Split('|');
                var data1 = fields[1];
                var data2 = int.Parse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture);
                var weight = decimal.Parse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture);
                var date = DateTime.ParseExact(fields[4], "MMddyy HHmmss", CultureInfo.InvariantCulture);

                // at this point, you can save the data to a database, write it to a file, send it to a cloud service, etc.

                // send an ACK (Y) to the device to tell it that everything was processed successfully.
                return $"SAMPLE_COMMAND|{ACK}|";
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }

            // if anything fails, reply to the device with a NAK (N).
            return $"SAMPLE_COMMAND|{NAK}|";
        }

        private string GetQueryDataCommandPattern()
        {
            var pattern = @"^QUERY" + GetFieldDelimiterPattern();
            pattern += GetUnsignedIntegerPattern(1, 10) + GetFieldDelimiterPattern();

            return pattern;
        }

        private string HandleQueryDataCommandMessage(string message)
        {
            //Handle the QUERY message from the indicator
            //QUERY|DATA1|
            //DATA1 = Audit Number to look up

            try
            {
                ServiceLog.Default.Log("TestLog");
                Log.WriteErrorLog(message);
                //writeToLog($"Message received: {message}");

                var fields = message.Split('|');
                var auditNumber = int.Parse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture);

                try
                {
                    if (repo.TransferOrderValid(auditNumber))
                    {

                        //writeToLog($"Transfer Order Id {auditNumber} was found and is in scheduled status");
                        Log.WriteErrorLog($"Transfer order id {auditNumber} was found and is in scheduled status");
                        return $"QUERY|{ACK}|";
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                    Log.WriteErrorLog($"An error occured looking up transfer order id {auditNumber}.");
                    Log.WriteErrorLog(ex);
                    //writeToLog($"Exception occurred while looking up audit number {auditNumber}.");
                    return $"QUERY|{NAK}|";
                }

                Log.WriteErrorLog($"transfer order id {auditNumber} was not found or not in scheduled status.");
                //writeToLog($"Transfer Order Id {auditNumber} was not found, or is not in scheduled status");
                return $"QUERY|{NAK}|"; ;
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog($"An exception has occurred in the Query method.");
                Log.WriteErrorLog(ex);
                Trace.WriteLine(ex);
            }

            return null;
        }

        private string GetInboundDataCommandPattern()
        {
            var pattern = @"^INBOUND" + GetFieldDelimiterPattern();
            pattern += GetUnsignedIntegerPattern(1, 10) + GetFieldDelimiterPattern();
            pattern += GetUnsignedIntegerPattern(1, 2) + GetFieldDelimiterPattern();
            pattern += GetUnsignedIntegerPattern(1, 10) + GetFieldDelimiterPattern();
            pattern += GetUnsignedIntegerPattern(1, 10) + GetFieldDelimiterPattern();
            pattern += GetUnsignedIntegerPattern(1, 10) + GetFieldDelimiterPattern();
            pattern += GetWeightPattern() + GetFieldDelimiterPattern();
            pattern += GetDateTimePattern() + GetFieldDelimiterPattern();

            return pattern;
        }

        private string HandleInboundCommandMessage(string message)
        {
            //Handle the inbound weight message
            //INBOUND|DATA1|DATA2|DATA3|DATA4|DATA5|DATA6|DATA7|
            //DATA1 = Audit number
            //DATA2 = Sequence number
            //DATA3 = Driver number
            //DATA4 = Truck number
            //DATA5 = Trailer number
            //DATA6 = Weight
            //DATA7 = Date Time in MMddyy HHmmss format

            var fields = message.Split('|');
            var auditNumber = int.Parse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture);
            var sequenceNumber = int.Parse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture);
            var driverNumber = int.Parse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture);
            var truckNumber = int.Parse(fields[4], NumberStyles.Integer, CultureInfo.InvariantCulture);
            var trailerNumber = int.Parse(fields[5], NumberStyles.Integer, CultureInfo.InvariantCulture);
            var scaleInWeight = decimal.Parse(fields[6], NumberStyles.Float, CultureInfo.InvariantCulture);
            var scaleInDate = DateTime.ParseExact(fields[7], "MMddyy HHmmss", CultureInfo.InvariantCulture);

            Log.WriteErrorLog($"Inbound message received for transfer order id {auditNumber}, sequence {sequenceNumber}");
            //writeToLog($"Inbound message received for Transfer Order Id {auditNumber} and sequence number {sequenceNumber}");
            try
            {
                if (repo.UpdateInboundScaleData(auditNumber, sequenceNumber, driverNumber, truckNumber, trailerNumber, scaleInWeight, scaleInDate))
                {
                    Log.WriteErrorLog($"Inbound weight updated successfully for transfer order id {auditNumber}, sequence {sequenceNumber}");
                    //writeToLog($"Inbound weight updated successfully for transfer order id {auditNumber} sequence number {sequenceNumber}");
                    return $"INBOUND|{ACK}|{DateTime.Now.ToString("MMddyy HHmmss")}";
                }
                else
                {
                    Log.WriteErrorLog($"Inbound weight was not updated for transfer order id {auditNumber}, sequence {sequenceNumber}.  Scale in record most likely already exists.");
                    //writeToLog($"Inbound weight did not update for transfer order id {auditNumber} sequence number {sequenceNumber}.  Scale in record most likely already exists");
                    return $"INBOUND|{NAK}|{DateTime.Now.ToString("MMddyy HHmmss")}";
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Exception occurred while trying to parse the inbound scale message for transfer order id {auditNumber} sequence number {sequenceNumber}");
                Trace.WriteLine(ex);
                Log.WriteErrorLog($"Exception occurred while trying to parse the inbound scale message for transfer order id {auditNumber} sequence number {sequenceNumber}");
                Log.WriteErrorLog(ex);
                //writeToLog($"Exception occurred while trying to parse the inbound scale message for transfer order id {auditNumber} sequence number {sequenceNumber}");
                //writeToLog(ex.Message);


                return $"INBOUND|{NAK}|{DateTime.Now.ToString("MMddyy HHmmss")}";
            }
        }

        private string GetOutboundDataCommandPattern()
        {
            var pattern = @"^OUTBOUND" + GetFieldDelimiterPattern();
            pattern += GetUnsignedIntegerPattern(1, 10) + GetFieldDelimiterPattern();
            pattern += GetUnsignedIntegerPattern(1, 2) + GetFieldDelimiterPattern();
            pattern += GetUnsignedIntegerPattern(1, 10) + GetFieldDelimiterPattern();
            pattern += GetWeightPattern() + GetFieldDelimiterPattern();
            pattern += GetDateTimePattern() + GetFieldDelimiterPattern();

            return pattern;
        }

        private string HandleOutboundCommandMessage(string message)
        {
            //Handle outbound data message
            //OUTBOUND|DATA1|DATA2|DATA3|DATA4|DATA5|
            //DATA1 = Audit number
            //DATA2 = Sequence number
            //DATA3 = Loader Id
            //DATA4 = Scale out weight
            //DATA5 = Scale out date in MMddyy HHmmss

            var fields = message.Split('|');
            var auditNumber = int.Parse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture);
            var sequenceNumber = int.Parse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture);
            var loaderId = int.Parse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture);
            var scaleOutWeight = decimal.Parse(fields[6], NumberStyles.Float, CultureInfo.InvariantCulture);
            var scaleOutDate = DateTime.ParseExact(fields[7], "MMddyy HHmmss", CultureInfo.InvariantCulture);

            Log.WriteErrorLog($"Outbound message received for transfer order id {auditNumber}, sequence {sequenceNumber}");
            //writeToLog($"Outbound weight message received for transfer order id {auditNumber} sequence number {sequenceNumber}");
            try
            {
                if (repo.UpdateOutboundScaleData(auditNumber, sequenceNumber, loaderId, scaleOutWeight, scaleOutDate))
                {
                    Log.WriteErrorLog($"Outbound weight updated successfully for transfer order id {auditNumber}, sequence {sequenceNumber}");
                    //writeToLog($"Outbound weight updated successfully for transfer order id {auditNumber} sequence number {sequenceNumber}");
                    return $"OUTBOUND|{ACK}|{DateTime.Now.ToString("MMddyy HHmmss")}";
                }
                else
                {
                    Log.WriteErrorLog($"Outbound weight was not updated for transfer order id {auditNumber}, sequence {sequenceNumber}.  Scale out record most likely already exists");
                    //writeToLog($"Outbound weight did not update for transfer order id {auditNumber} sequence number {sequenceNumber}.  Scale out record most likely already exists");
                    return $"OUTBOUND|{NAK}|{DateTime.Now.ToString("MMddyy HHmmss")}";
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Exception occurred while trying to parse the outbound scale message for transfer order id {auditNumber} sequence number {sequenceNumber}");
                Trace.WriteLine(ex);
                Log.WriteErrorLog($"Exception occurred while trying to parse the outbound scale message for transfer order id {auditNumber} sequence number {sequenceNumber}");
                Log.WriteErrorLog(ex);
                //writeToLog($"Exception occurred while trying to parse the outbound scale message for transfer order id {auditNumber} sequence number {sequenceNumber}");
                //writeToLog(ex.Message);

                return $"OUTBOUND|{NAK}|{DateTime.Now.ToString("MMddyy HHmmss")}";
            }
        }

        //private void writeToLog(string message)
        //{
        //    if (commLog == null)
        //        ServiceLog.Default.Log(message);
        //    else
        //        commLog.Log(message);
        //}
    }
}
