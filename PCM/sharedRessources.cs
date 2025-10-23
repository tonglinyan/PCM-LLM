using PCM;
public static class SharedResources
{
    public static SemaphoreSlim semaphore = new(0, 1);
    public static Manager manager = new();
    //public
}