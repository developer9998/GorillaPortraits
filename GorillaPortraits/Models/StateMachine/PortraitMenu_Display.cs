using GorillaPortraits.Behaviours.Objects;

namespace GorillaPortraits.Models.StateMachine
{
    public class PortraitMenu_Display(Portrait portrait) : PortraitMenu_Base(portrait)
    {
        public override void Enter()
        {
            base.Enter();

            Photo currentPhoto = portrait.currentPhoto;
            portrait.backgroundImage.color = currentPhoto.BackgroundColour;
            portrait.photoImage.sprite = currentPhoto.Sprite;
            portrait.photoImage.color = currentPhoto.Colour;
        }
    }
}
