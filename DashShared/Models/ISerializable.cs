namespace DashShared
{
    //THIS IS EMPTY SO FAR BUT STILL IMPORTANT
    public interface ISerializable
    {
        //We could have methods in here that will simply serialzize this object.  
        //Then, for the sake of testing, we can call those method is Debug statements to see if it fails.
        //These methods would simply check to see if deserializing and reserializng returns the identtical objects
    }
}
