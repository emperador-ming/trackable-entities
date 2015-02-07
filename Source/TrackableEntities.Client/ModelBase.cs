﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace TrackableEntities.Client
{
    /// <summary>
    /// Base class for model entities to support IIdentifiable and INotifyPropertyChanged
    /// </summary>
    /// <typeparam name="TModel">Entity type</typeparam>
    [DataContract(IsReference = true)]
    public abstract class ModelBase<TModel> : IIdentifiable, INotifyPropertyChanged
    {
        /// <summary>
        /// Event for notification of property changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Fire PropertyChanged event.
        /// </summary>
        /// <typeparam name="TResult">Property return type</typeparam>
        /// <param name="property">Lambda expression for property</param>
        protected virtual void NotifyPropertyChanged<TResult>
            (Expression<Func<TModel, TResult>> property)
        {
            string propertyName = ((MemberExpression)property.Body).Member.Name;
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Generate entity identifier used for correlation with MergeChanges (if not yet done)
        /// </summary>
        public void SetEntityIdentifier()
        {
            if (GetEntityIdentifier(this).Equals(default(Guid)))
            {
                SetEntityIdentifier(this, Guid.NewGuid());
            }
        }

        /// <summary>
        /// Set entity identifier used for correlation with MergeChanges
        /// </summary>
        /// <param name="value">Identifier of some other entity</param>
        public void SetEntityIdentifier(Guid value)
        {
            SetEntityIdentifier(this, value);
        }

        /// <summary>
        /// Copy entity identifier used for correlation with MergeChanges from another entity
        /// </summary>
        /// <param name="other">Other trackable object</param>
        public void SetEntityIdentifier(IIdentifiable other)
        {
            SetEntityIdentifier(this, GetEntityIdentifier(other));
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same
        /// type. The comparison is based on EntityIdentifier.
        /// 
        /// If the local EntityIdentifier is empty, then return false.
        /// </summary>
        /// <param name="other">An object to compare with this object</param>
        public bool IsEquatable(IIdentifiable other)
        {
            var method = GetEquatableMethod(this);
            if (method != null)
            {
                return (bool)method.Invoke(this, new object[] { other });
            }
            return false;
        }

        bool IEquatable<IIdentifiable>.Equals(IIdentifiable other)
        {
            return IsEquatable(other);
        }

        private object GetEntityIdentifier(IIdentifiable entity)
        {
            var property = GetEntityIdentifierProperty(entity);

            if (property == null)
                return default(Guid);
            else
                return (Guid)property.GetValue(entity, null);
        }

        private void SetEntityIdentifier(IIdentifiable entity, object identifier)
        {
            var property = GetEntityIdentifierProperty(entity);

            if (property != null)
                property.SetValue(entity, identifier, null);
        }

        private static MethodInfo GetEquatableMethod(object obj)
        {
            Type type = obj.GetType();
            string equatableMethod =
                Constants.EquatableMembers.EquatableMethodStart +
                type.FullName +
                Constants.EquatableMembers.EquatableMethodEnd;
            var method = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .SingleOrDefault(m => m.Name == equatableMethod);
            return method;
        }

        private static PropertyInfo GetEntityIdentifierProperty(object obj)
        {
            var property = obj.GetType().BaseTypes()
                .SelectMany(t => t.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic))
                .SingleOrDefault(m => m.Name == Constants.EquatableMembers.EntityIdentifierProperty);
            return property;
        }

        private static class Constants
        {
            /// <summary>
            /// Change-tracking property names.
            /// </summary>
            public static class TrackingProperties
            {
                /// <summary>TrackingState property name</summary>
                public const string TrackingState = "TrackingState";
                /// <summary>ModifiedProperties property name</summary>
                public const string ModifiedProperties = "ModifiedProperties";
            }

            /// <summary>
            /// Equatable member names.
            /// </summary>
            public static class EquatableMembers
            {
                /// <summary>Equatable method start</summary>
                public const string EquatableMethodStart = "System.IEquatable<";

                /// <summary>Equatable method end</summary>
                public const string EquatableMethodEnd = ">.Equals";

                /// <summary>Entity identifier property</summary>
                public const string EntityIdentifierProperty = "EntityIdentifier";
            }
        }
    }
}
