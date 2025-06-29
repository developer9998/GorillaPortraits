using GorillaPortraits.Behaviours.Objects;

namespace GorillaPortraits.Models.StateMachine
{
    public class PortraitMenu_Base(Portrait portrait) : State
    {
        protected Portrait portrait = portrait;
    }
}
