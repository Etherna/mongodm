namespace Digicando.MongoDM.Utility
{
    internal interface ILocalContextAccessor
    {
        LocalContext Context { get; }

        void OnCreated(LocalContext context);

        void OnDisposed(LocalContext context);
    }
}