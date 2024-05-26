namespace ICENet.Traffic
{
    public abstract class Packet
    {
        public abstract int Id { get; }

        public abstract bool IsReliable { get; }

        public void Serialize(ref Buffer data)
        {
            data.Write(Id);

            Write(ref data);
        }

        protected virtual void Write(ref Buffer data) { }

        public void Deserialize(ref Buffer data)
        {
            Read(ref data);
        }

        protected virtual void Read(ref Buffer data) { }
    }
}
