using GorillaPortraits.Behaviours.Objects;

namespace GorillaPortraits.Models.StateMachine
{
    public class PortraitState_Base(Portrait portrait) : State
    {
        protected Portrait portrait = portrait;
    }
}