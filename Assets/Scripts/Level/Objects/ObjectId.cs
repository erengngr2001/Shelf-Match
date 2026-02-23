using System;

namespace Level.Objects
{
    public readonly struct ObjectId : IEquatable<ObjectId>
    {
        public string Value { get; }

        public ObjectId(string value)
        {
            Value = value;
        }

        public override string ToString() => Value;
        public bool Equals(ObjectId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ObjectId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        
        public static bool operator ==(ObjectId left, ObjectId right) => left.Equals(right);
        public static bool operator !=(ObjectId left, ObjectId right) => !(left == right);
    }
}
