namespace MiniFactory.Core.Contracts
{
    /// <summary>Хранение прогресса. Реализация определяется снаружи (файл/облако).</summary>
    public interface ISaveService
    {
        bool HasProgress();

        /// <summary> null — сохранения нет или оно повреждено.</summary>
        Save.PlayerProgress Load();

        void Save(Save.PlayerProgress progress);

        void Delete();
    }
}
