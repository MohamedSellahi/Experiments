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

            var people = db.Person.FilterBy(person).ToList(); 

            foreach (var p in people)
            {
               Console.WriteLine("FName: {0} --------> LName: {1}", p.FirstName, p.LastName);
            }
         }
      }
   }
}
