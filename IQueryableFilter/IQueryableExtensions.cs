﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IQueryableFilter
{
   public static class IQueryableExtensions
   {
      /// <summary>
      /// applies the filter <paramref name="filter"/> to the collection <paramref name="collection"/>.
      /// The method considers simple properties only
      /// </summary>
      /// <typeparam name="T">the type of the entity in the <paramref name="collection"/></typeparam>
      /// <param name="collection">the collection of items to be filtered</param>
      /// <param name="filter">the filter to apply on the collection</param>
      /// <param name="DisCardNullArguments">specifies if the filtering should ignore unspecified properties of the filter</param>
      /// <returns></returns>
      public static IQueryable<T> FilterBy<T>(this IQueryable<T> collection, T filter, bool DisCardNullArguments = true)
      {
                  // build the where clause 
         Expression<Func<T, bool>> whereClause = BuildWhereClause(filter, DisCardNullArguments);
         return whereClause == null ? collection : collection.Where(whereClause); 
      }

      /// <summary>
      /// Creates the 
      /// </summary>
      /// <typeparam name="T">the type of the filter</typeparam>
      /// <param name="filter">the filter to be applied</param>
      /// <param name="DisCardNullArguments">if true, properties with null values are discarded in 
      /// the where the generated where clause</param>
      /// <returns></returns>
      public static Expression<Func<T, bool>> BuildWhereClause<T>(T filter, bool DisCardNullArguments = true)
      {

         return DisCardNullArguments ?
            BuildWhereClaseDiscardNullValuedProperties(filter) : 
            BuildWhereClaseKeepNullValuedProperties(filter);
      }

      /// <summary>
      /// builds an expression for the where clause. default values of each property 
      /// are neglected like this : T.property == Defaul(property) || T.property == filter.property
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="filter"></param>
      /// <returns></returns>
      public static Expression<Func<T, bool>> BuildWhereClaseDiscardNullValuedProperties<T>(T Filter)
      {
         if (Filter == null)
         {
            throw new ArgumentNullException("Filter");
         }
         // get the properties of T 
         var type = typeof(T);
         // get simple properties, discard complex types and navigation properties
         var properties = type.GetProperties().Where(p => IsSimple(p)).ToList();
         if (properties.Count == 0)
         {
            throw new ArgumentException("The passed type does not have any properties. Can't define a filter expression");
         }

         List<Expression> leftTerms = new List<Expression>();
         List<Expression> rightTerms = new List<Expression>();

         var parameter = Expression.Parameter(typeof(T));

         // construct the condition terms : (T.property == filter.property || T.property == Default(p))
         foreach (var p in properties)
         {
            Expression leftEqual = Expression.PropertyOrField(parameter, p.Name);
            Expression rightEqual = Expression.Constant(p.GetValue(Filter));
            Expression LeftOr = GetEqualsWithValue(leftEqual, rightEqual,p);
            Expression RigthOr = GetEqualsWithDefault(leftEqual, p);

            leftTerms.Add(LeftOr);
            rightTerms.Add(RigthOr);
         }
         Expression whereClause = Expression.Constant(true); // default filter 
         if (properties.Count > 0)
         {
            whereClause = Expression.Or(leftTerms[0], rightTerms[0]);
         }
         for (int i = 1; i < properties.Count; i++)
         {
            whereClause = Expression.And(whereClause, Expression.Or(leftTerms[i], rightTerms[i]));
         }

         return Expression.Lambda<Func<T, bool>>(whereClause, parameter);
      }

      /// <summary>
      /// builds an expression for the where clause using the filter. 
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="Filter"></param>
      /// <returns></returns>
      public static Expression<Func<T, bool>> BuildWhereClaseKeepNullValuedProperties<T>(T Filter)
      {
         if (Filter == null)
         {
            throw new ArgumentNullException("Filter");
         }
         // get the properties of T 
         var type = typeof(T);
         // get simple properties, discard complex types and navigation properties
         var properties = type.GetProperties().Where(p => IsSimple(p)).ToList();
         if (properties.Count == 0)
         {
            throw new ArgumentException("The passed type does not have any properties. Can't define a filter expression");
         }

         List<Expression> leftTerms = new List<Expression>();
         List<Expression> rightTerms = new List<Expression>();

         var parameter = Expression.Parameter(typeof(T));

         // construct the condition terms : (T.property == filter.property)
         foreach (var p in properties)
         {
            Expression left = Expression.PropertyOrField(parameter, p.Name);
            Expression right = Expression.Constant(p.GetValue(Filter));

            // this is necessary for nullable value so that comparaison operators work correctly
            if (!IsNullableValueType(right.Type) && IsNullableValueType(left.Type))
            {
               right = Expression.Convert(right, p.PropertyType);
            }
            else if (((ConstantExpression)right).Value == null)
            {
               right = Expression.Convert(right, p.PropertyType);
            }


            leftTerms.Add(left);
            rightTerms.Add(right);
         }
         Expression whereClause = Expression.Constant(true); // default filter 
         if (properties.Count > 0)
         {
            whereClause = Expression.Equal(leftTerms[0], rightTerms[0]);
         }
         for (int i = 1; i < properties.Count; i++)
         {
            whereClause = Expression.And(whereClause, Expression.Equal(leftTerms[i], rightTerms[i]));
         }

         return Expression.Lambda<Func<T, bool>>(whereClause, parameter);
      }

      /// <summary>
      /// returns a term representing equality with default value of the property type 
      /// </summary>
      /// <param name="left"></param>
      /// <param name="right"></param>
      /// <param name="p"></param>
      /// <returns></returns>
      public static Expression GetEqualsWithDefault(Expression left, PropertyInfo p)
      {
         return Expression.Equal(left, Expression.Constant(GetDefaultValue(p.PropertyType)));
      }

      /// <summary>
      /// gets the and term like this : T.property == filter.property
      /// </summary>
      /// <param name="left"></param>
      /// <param name="right"></param>
      /// <returns></returns>
      public static Expression GetEqualsWithValue(Expression left, Expression right, PropertyInfo p)
      {
         // this is necessary for nullable value so that comparaison operators work correctly
         if (!IsNullableValueType(right.Type) && IsNullableValueType(left.Type))
         {
            right = Expression.Convert(right, p.PropertyType);
         }
         else if (((ConstantExpression)right).Value == null)
         {
            right = Expression.Convert(right, p.PropertyType);
         }

         return Expression.Equal(left, right); 
      }

      /// <summary>
      /// checks if the property is a simple property 
      /// </summary>
      /// <param name="p"></param>
      /// <returns></returns>
      public static bool IsSimple(PropertyInfo p)
      {
         var propertyType = p.PropertyType;
         return propertyType.IsPrimitive
            || propertyType.Equals(typeof(string))
            || propertyType.Equals(typeof(decimal))
            || IsNullableValueType(propertyType); 
      }

      /// <summary>
      /// Checks if the type if a nullable value type
      /// </summary>
      /// <param name="type">The type to test</param>
      /// <returns></returns>
      static bool IsNullableValueType(Type type)
      {
         return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
      }

      /// <summary>
      /// Gets the default value for a type 
      /// </summary>
      /// <param name="type"></param>
      /// <returns></returns>
      static object GetDefaultValue(Type type)
      {
         return type.IsValueType ? Activator.CreateInstance(type): null;
      }

      /// <summary>
      /// gets the default value of a type
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <returns></returns>
      public static T GetDefaultValue<T>()
      {
         return default(T);
      }
      //test 
      public static void PrintProperties(Type type)
      {
         foreach (var prop in type.GetProperties().OrderBy(p=>p.Name))
         {
            Console.WriteLine("Name: {0}, ===> type: {1}, ==> IsSimple: {2}"
               ,prop.Name, prop.PropertyType.Name, IsSimple(prop));
         }
      }
   }
}
