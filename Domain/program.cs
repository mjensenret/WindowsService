using Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    class Program
    {
        private static TransferOrderRepository repo = new TransferOrderRepository();

        static void Main(string[] args)
        {
            using (var db = new TestTransferContext())
            {
                var query = from b in db.TransferOrders
                            where b.transferOrderId > 64000
                            where b.status == "OPEN"
                            select b;

                Console.WriteLine("Query TransferOrder Arrivals");

                int rowCount = 1;
                foreach (var item in query)
                {

                    Console.WriteLine("Row: {0} - ID: {1}", rowCount.ToString(), item.transferOrderId.ToString());
                    rowCount += 1;
                }
                bool found = repo.TransferOrderValid(64005);
                if (found)
                    Console.WriteLine("We found order Id: {0}", Convert.ToString(64005));
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();


            }
        }
    }
}
