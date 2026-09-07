namespace Base
{
    public interface IBasic<T>
    {
        public void UpdateValues(T newValue)
        {
        }

        public T GetValues();
    }

    public interface IResource<T> : IBasic<T>
    {
    }

    public interface IStats<T> : IBasic<T>
    {
        public T MaxValue();
    }

    public interface ISettings<T> : IBasic<T>
    {
        public SettingsType GetSettingsType();
    }

    public abstract record UIAction
    {
        public record UpdateNewValue<T>(T Type, int NewValue) : UIAction;
        public record DoneWithNothing() : UIAction;
        public record FalseWithNothing() : UIAction;
    }

    public enum SettingsType
    {
        Music,
        Sound,
        Voice,
        Language,
        Screen,
    }
}