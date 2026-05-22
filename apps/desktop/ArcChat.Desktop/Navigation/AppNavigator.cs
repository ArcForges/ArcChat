// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Desktop.Navigation;

internal sealed class AppNavigator : IAppNavigator
{
    private readonly Stack<Destination> backStack = new Stack<Destination>();
    private readonly Stack<Destination> forwardStack = new Stack<Destination>();
    private readonly ObservableDestination currentDestination;

    public AppNavigator()
    {
        this.currentDestination = new ObservableDestination(new Home());
    }

    public IObservable<Destination> CurrentDestination => this.currentDestination;

    public Destination Current => this.currentDestination.Value;

    public void Navigate(Destination destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (destination == this.Current)
        {
            return;
        }

        this.backStack.Push(this.Current);
        this.forwardStack.Clear();
        this.currentDestination.Publish(destination);
    }

    public bool Back()
    {
        if (!this.backStack.TryPop(out Destination? previous))
        {
            return false;
        }

        this.forwardStack.Push(this.Current);
        this.currentDestination.Publish(previous);
        return true;
    }

    public bool Forward()
    {
        if (!this.forwardStack.TryPop(out Destination? next))
        {
            return false;
        }

        this.backStack.Push(this.Current);
        this.currentDestination.Publish(next);
        return true;
    }

    private sealed class ObservableDestination : IObservable<Destination>
    {
        private readonly List<IObserver<Destination>> observers = new List<IObserver<Destination>>();

        public ObservableDestination(Destination initialDestination)
        {
            this.Value = initialDestination;
        }

        public Destination Value { get; private set; }

        public IDisposable Subscribe(IObserver<Destination> observer)
        {
            ArgumentNullException.ThrowIfNull(observer);
            this.observers.Add(observer);
            observer.OnNext(this.Value);
            return new Subscription(this.observers, observer);
        }

        public void Publish(Destination destination)
        {
            this.Value = destination;
            foreach (IObserver<Destination> observer in this.observers.ToArray())
            {
                observer.OnNext(destination);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly List<IObserver<Destination>> observers;
            private readonly IObserver<Destination> observer;

            public Subscription(List<IObserver<Destination>> observers, IObserver<Destination> observer)
            {
                this.observers = observers;
                this.observer = observer;
            }

            public void Dispose()
            {
                _ = this.observers.Remove(this.observer);
            }
        }
    }
}
