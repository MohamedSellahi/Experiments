using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace IQueryableFilter
{
   class Program
   {
      static void Main(string[] args)
      {
         IQueryable<int> collection = (new int[] { 1, 3, 4 }).AsQueryable();

         using (var db = new DefaultContext())
         {
            var person = db.Person.FirstOrDefault();
            var people = db.Person.Where(p=>p.Demographics == "<IndividualSurvey xmlns=\"http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/IndividualSurvey\"><TotalPurchaseYTD>0</TotalPurchaseYTD></IndividualSurvey>");

            //var query1 = db.Person.FilterBy(person);
            //var people = query1.ToList();

            foreach (var p in people)
            {
               Console.WriteLine("FName: {0} --------> LName: {1}", p.FirstName, p.LastName);
            }

            var address = db.Address.FirstOrDefault();
            Console.WriteLine("Address: {0} --> {1}", address.AddressLine1, address.City);
            Console.WriteLine("------------------");
            var testList = db.Address.Take(10);
            foreach (var item in testList)
            {
               Console.WriteLine("Address: {0} --> {1}", item.AddressLine1, item.City);
            }
            Console.WriteLine("------------------");
            Console.WriteLine("Filtered : ");
            var query = db.Address.FilterBy(address);
            var ad = query.ToList();

            foreach (var item in ad)
            {
               Console.WriteLine("Address: {0} --> {1}", item.AddressLine1, item.City);
            }
         }
      }
   }
}
