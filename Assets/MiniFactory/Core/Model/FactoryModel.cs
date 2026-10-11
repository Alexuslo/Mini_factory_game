using System;
using MiniFactory.Core.Config;
using MiniFactory.Core.Save;

namespace MiniFactory.Core.Model
{
    /// <summary>
    /// Игровое состояние фабрики: баланс, машины, активный Boost.
    /// Единственный владелец данных — всё изменение идёт через него,
    /// изменения оповещаются событиями (MVC: Model → View).
    /// </summary>
    public sealed class FactoryModel
    {
        private readonly GameConfig _config;

        public double Balance { get; private set; }

        public MachineState[] Machines { get; }

        /// <summary>Момент окончания активного Boost (Unix); 0 — не активен.</summary>
        public long BoostEndUnix { get; private set; }

        public long BoostStartedUnix { get; private set; }

        public int TutorialStep { get; set; }

        // Model → View
        public event Action<double> OnBalanceChanged;
        public event Action<MachineState> OnMachineChanged;
        public event Action<long> OnBoostChanged;
        public event Action<int> OnTutorialStepChanged;

        public FactoryModel(GameConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            var machines = config.Machines ?? Array.Empty<MachineConfig>();
            Machines = new MachineState[machines.Length];
            for (var i = 0; i < machines.Length; i++)
            {
                // unlockCost == 0 → машина доступна сразу (ТЗ п.1)
                var unlocked = machines[i].UnlockCost == 0;
                Machines[i] = new MachineState
                {
                    Id = machines[i].Id,
                    IsUnlocked = unlocked,
                    Level = unlocked ? 1 : 0
                };
            }
        }

        public MachineState GetMachine(string id)
        {
            for (var i = 0; i < Machines.Length; i++)
                if (Machines[i].Id == id) return Machines[i];
            return null;
        }

        public void AddBalance(double amount)
        {
            if (amount <= 0) return;
            Balance += amount;
            OnBalanceChanged?.Invoke(Balance);
        }

        /// <summary>Списывает валюту. false — недостаточно средств, ничего не меняется.</summary>
        public bool TrySpend(double amount)
        {
            if (amount < 0 || amount > Balance) return false;
            Balance -= amount;
            OnBalanceChanged?.Invoke(Balance);
            return true;
        }

        public void UnlockMachine(string id)
        {
            var machine = GetMachine(id);
            if (machine == null || machine.IsUnlocked) return;
            machine.IsUnlocked = true;
            machine.Level = 1;
            OnMachineChanged?.Invoke(machine);
        }

        public void SetLevel(string id, int level)
        {
            var machine = GetMachine(id);
            if (machine == null || !machine.IsUnlocked) return;
            machine.Level = level;
            OnMachineChanged?.Invoke(machine);
        }

        public void SetBoost(long startedUnix, long endUnix)
        {
            BoostStartedUnix = startedUnix;
            BoostEndUnix = endUnix;
            OnBoostChanged?.Invoke(endUnix);
        }

        public bool IsBoostActive(long nowUnix) => nowUnix < BoostEndUnix;

        /// <summary>Шаг мини-туториала; продвигается только вперёд, оповещает View.</summary>
        public void SetTutorialStep(int step)
        {
            if (TutorialStep == step) return;
            TutorialStep = step;
            OnTutorialStepChanged?.Invoke(step);
        }

        /// <summary>Восстановление состояния из сохранения (сырые данные уже прочитаны).</summary>
        public void Restore(PlayerProgress progress)
        {
            if (progress == null) return;

            Balance = progress.balance;
            TutorialStep = progress.tutorialStep;
            BoostStartedUnix = progress.boostStartedUnix;
            BoostEndUnix = progress.boostEndUnix;

            if (progress.machines == null) return;
            for (var i = 0; i < progress.machines.Length; i++)
            {
                var saved = progress.machines[i];
                var machine = GetMachine(saved.id);
                if (machine == null) continue; // машина исчезла из конфига — запись игнорируем
                machine.IsUnlocked = saved.unlocked;
                machine.Level = saved.level;
            }

            // после восстановления сообщаем View, что данные готовы
            OnBalanceChanged?.Invoke(Balance);
            OnBoostChanged?.Invoke(BoostEndUnix);
            foreach (var m in Machines) OnMachineChanged?.Invoke(m);
        }
    }
}
