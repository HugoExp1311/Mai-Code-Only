using System.Threading.Tasks;
using Base.Character.Action;
using Base.Character.Stats;
using Base.Inventory.Item;

namespace Base.Character
{
    public interface ICharacter
    {
        public string GetName();

        public string Talk()
        {
            return "";
        }

        public Gender GetGender();
        public Task<UIAction> DoAction(CharacterActions actions);
    }

    public interface IPlayer : ICharacter
    {
        Gender ICharacter.GetGender()
        {
            return Gender.Male;
        }
    }

    public interface ITarget : ICharacter
    {
        Gender ICharacter.GetGender()
        {
            return Gender.Female;
        }
    }

    public enum Gender
    {
        Male = 0,
        Female = 1
    }
}