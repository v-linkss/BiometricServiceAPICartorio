using System;
using System.Collections.Generic;

namespace BiometricService.Observers
{
    public interface IObserver<T>
    {
        void Update(T file);
    }

    public interface IObservable<T>
    {
        void Register(IObserver<T> observer);
        void Unregister(IObserver<T> observer);
        void Notify(T file);
    }

    public class UploadFileObservable : IObservable<UploadFile>
    {
        private readonly List<IObserver<UploadFile>> _observers = new List<IObserver<UploadFile>>();

        public void Register(IObserver<UploadFile> observer)
        {
            _observers.Add(observer);
        }

        public void Unregister(IObserver<UploadFile> observer)
        {
            _observers.Remove(observer);
        }

        public void Notify(UploadFile file)
        {
            foreach (var observer in _observers)
            {
                observer.Update(file);
            }
        }
    }
}