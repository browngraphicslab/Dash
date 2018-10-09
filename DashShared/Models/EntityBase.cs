using Newtonsoft.Json;

namespace DashShared
{
    /// <summary>
    /// the base class for anything that is stored in the Database, with few exceptions every model
    /// should inherit from this class.
    /// </summary>
    public abstract class EntityBase : ISerializable
    {

        protected EntityBase(string id = null)
        {
            Id = id ?? UtilShared.GenerateNewId();

        }

        private string _id;
        /// <summary>
        /// Object unique identifier
        /// </summary>
        [JsonProperty("id")] // serialized as id (for documentdb)
        public string Id { get => _id; protected set => _id = value.ToLower(); }

        /// <summary>
        /// Pretty print the key for debugging purposes
        /// </summary>
        /// <returns>The well formatted string</returns>
        public override string ToString()
        {
            return $"Id:{Id}" + base.ToString();
        }

        protected bool Equals(EntityBase other)
        {
            return string.Equals(Id, other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((EntityBase) obj);
        }


        public virtual bool ValueEquals(EntityBase otherModel)
        {
            var tempId = Id;
            Id = "";
            var copy = otherModel.Clone();
            copy.Id = "";
            var equal =  string.Equals(this.Serialize(), copy.Serialize());
            Id = tempId;
            return equal;
        }

        public override int GetHashCode()
        {
            return (Id != null ? Id.GetHashCode() : 0);
        }
    }
}
