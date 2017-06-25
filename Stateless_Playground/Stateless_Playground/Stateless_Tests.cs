using NUnit.Framework;
using Stateless;

namespace Stateless_Playground
{
    [TestFixture]
    public class Stateless_Tests
    {
        private StateMachine<State, Trigger> phoneCall;

        enum State
        {
            OffHook = 0,
            Ringing,
            Connected,
            OnHold
        }

        enum Trigger
        {
            CallDialled = 0,
            CallConnected,
            LeftMessage,
            PlacedOnHold
        }

        class ActionsCalled
        {
            public bool Ringing { get; set; }
            public bool ErrorOccurred { get; set; }
        }

        ActionsCalled CalledActions { get; set; } = new ActionsCalled();

        [SetUp]
        public void Setup()
        {

        }


        [Test]
        public void CheckIfRinginCallIsCalled()
        {
            this.phoneCall = new StateMachine<State, Trigger>(State.OffHook);

            phoneCall.Configure(State.OffHook)
                .Permit(Trigger.CallDialled, State.Ringing);

            phoneCall.Configure(State.Ringing)
                .Permit(Trigger.CallConnected, State.Connected)
                .OnEntry(() => this.CalledActions.Ringing = true);

            phoneCall.Configure(State.Connected)
                .Permit(Trigger.LeftMessage, State.OffHook)
                .Permit(Trigger.PlacedOnHold, State.OnHold);


            this.phoneCall.Fire(Trigger.CallDialled);
            Assert.IsTrue(this.CalledActions.Ringing);
        }

        [Test]
        public void CheckIgnoreFeature_RingingCallbackIsOnlyCalledONCE()
        {
            this.phoneCall = new StateMachine<State, Trigger>(State.OffHook);

            phoneCall.Configure(State.OffHook)
                .Permit(Trigger.CallDialled, State.Ringing);

            phoneCall.Configure(State.Ringing)
                .Permit(Trigger.CallConnected, State.Connected)
                .OnEntry(() => this.CalledActions.Ringing = true)
                .Ignore(Trigger.CallDialled);

            phoneCall.Configure(State.Connected)
                .Permit(Trigger.LeftMessage, State.OffHook)
                .Permit(Trigger.PlacedOnHold, State.OnHold);

            System.Console.WriteLine(phoneCall.ToDotGraph()); // Dump statemachine to testrunner output


            this.phoneCall.Fire(Trigger.CallDialled);
            Assert.IsTrue(this.CalledActions.Ringing);

            this.CalledActions.Ringing = false;

            // Second call should not trigger ringing callback!!!
            this.phoneCall.Fire(Trigger.CallDialled);
            Assert.IsFalse(this.CalledActions.Ringing);
        }

        [Test]
        public void CheckReentrantFeature_RingingCallbackIsOnlyCalledOnReentrance()
        {
            this.phoneCall = new StateMachine<State, Trigger>(State.OffHook);

            phoneCall.Configure(State.OffHook)
                .Permit(Trigger.CallDialled, State.Ringing);

            phoneCall.Configure(State.Ringing)
                .Permit(Trigger.CallConnected, State.Connected)
                .OnEntry(() => this.CalledActions.Ringing = true)
                .PermitReentry(Trigger.CallDialled);

            phoneCall.Configure(State.Connected)
                .Permit(Trigger.LeftMessage, State.OffHook)
                .Permit(Trigger.PlacedOnHold, State.OnHold);
            System.Console.WriteLine(phoneCall.ToDotGraph()); // Dump statemachine to testrunner output


            this.phoneCall.Fire(Trigger.CallDialled);
            Assert.IsTrue(this.CalledActions.Ringing);

            this.CalledActions.Ringing = false;

            // Second call should not trigger ringing callback!!!
            this.phoneCall.Fire(Trigger.CallDialled);
            Assert.IsTrue(this.CalledActions.Ringing);
        }



        [Test]
        public void StateMachineThrowsExceptionDueNotAllowedTransition()
        {
            this.phoneCall = new StateMachine<State, Trigger>(State.OffHook);

            phoneCall.Configure(State.OffHook)
                .Permit(Trigger.CallDialled, State.Ringing);

            phoneCall.Configure(State.Ringing)
                .Permit(Trigger.CallConnected, State.Connected)
                .OnEntry(() => this.CalledActions.Ringing = true);

            phoneCall.Configure(State.Connected)
                .Permit(Trigger.LeftMessage, State.OffHook)
                .Permit(Trigger.PlacedOnHold, State.OnHold);
            System.Console.WriteLine(phoneCall.ToDotGraph()); // Dump statemachine to testrunner output

            Assert.Throws(typeof(System.InvalidOperationException), () => this.phoneCall.Fire(Trigger.PlacedOnHold));
        }

        [Test]
        public void StateMachineDontThrowsExceptionItCallsErrorCallbackInstead()
        {
            // ARRANGE
            this.phoneCall = new StateMachine<State, Trigger>(State.OffHook);

            phoneCall.Configure(State.OffHook)
                .Permit(Trigger.CallDialled, State.Ringing);

            phoneCall.Configure(State.Ringing)
                .Permit(Trigger.CallConnected, State.Connected)
                .OnEntry(() => this.CalledActions.Ringing = true);

            phoneCall.Configure(State.Connected)
                .Permit(Trigger.LeftMessage, State.OffHook)
                .Permit(Trigger.PlacedOnHold, State.OnHold);

            phoneCall.OnUnhandledTrigger((state, trigger) => this.CalledActions.ErrorOccurred = true);
            System.Console.WriteLine(phoneCall.ToDotGraph()); // Dump statemachine to testrunner output

            // ACT
            this.phoneCall.Fire(Trigger.PlacedOnHold);

            Assert.IsTrue(this.CalledActions.ErrorOccurred);
        }
    }
}
