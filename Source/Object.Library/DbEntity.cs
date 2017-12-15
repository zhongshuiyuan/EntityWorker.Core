﻿using System.Runtime.Serialization;
using EntityWorker.Core.Attributes;
using EntityWorker.Core.Helper;
using EntityWorker.Core.InterFace;
using FastDeepCloner;

namespace EntityWorker.Core.Object.Library
{
    public class DbEntity : IDbEntity
    {
        public event Events.IdChanged OnIdChanged;

        private long _id;

        [PrimaryKey]
        public virtual long Id
        {
            get => _id;
            set
            {
                if (value != _id)
                {
                    _id = value;
                    OnIdChanged?.Invoke(_id);
                }
                else
                    _id = value;
            }
        }

        [ExcludeFromAbstract]
        public virtual ItemState State { get; set; }

        public DbEntity() { }

        /// <inheritdoc />
        /// <summary>
        /// Clone the object
        /// </summary>
        /// <param name="fieldType"></param>
        /// <returns></returns>
        public IDbEntity Clone(FieldType fieldType = FieldType.PropertyInfo)
        {
            return DeepCloner.Clone(this, new FastDeepClonerSettings()
            {
                FieldType = fieldType,
                OnCreateInstance = new Extensions.CreateInstance(FormatterServices.GetUninitializedObject)
            });
        }

    }
}