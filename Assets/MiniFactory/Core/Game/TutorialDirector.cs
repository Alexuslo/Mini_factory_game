using System;
using MiniFactory.Core.Config;
using MiniFactory.Core.Model;

namespace MiniFactory.Core.Game
{
    /// <summary>
    /// Продвижение мини-туториала по data-driven шагам из конфига.
    /// Не знает про View: целевую машину резолвит по шагу, View подписывается на события модели.
    /// Создаёт и привязывает composition root.
    /// </summary>
    public sealed class TutorialDirector
    {
        private readonly FactoryModel _model;
        private readonly GameConfig _config;

        /// <summary>Id туториала — из конфига; по нему композер раздаёт директора потребителям.</summary>
        public string Id => _config.TutorialId;

        public TutorialDirector(FactoryModel model, GameConfig config)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Bind(GameController controller)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            controller.MachineUpgraded += NotifyUpgraded;
            controller.MachineUnlocked += NotifyUnlocked;
        }

        /// <summary>Дёргается контроллером (или тестом) при успешном апгрейде.</summary>
        public void NotifyUpgraded(string machineId)
            => TryAdvance(TutorialTrigger.UpgradeAny, TutorialTrigger.UpgradeMachine, machineId);

        /// <summary>Дёргается контроллером (или тестом) при успешном открытии.</summary>
        public void NotifyUnlocked(string machineId)
            => TryAdvance(TutorialTrigger.UnlockAny, TutorialTrigger.UnlockMachine, machineId);

        /// <summary>Текущий шаг; null — туториал завершён (или шагов нет).</summary>
        public TutorialStep CurrentStep
        {
            get
            {
                var steps = _config.Tutorial;
                var i = _model.TutorialStep;
                return steps != null && i >= 0 && i < steps.Length ? steps[i] : null;
            }
        }

        /// <summary>Id машины, на которую указывает текущий шаг; null — скрыть.</summary>
        public string ResolveTargetMachineId()
        {
            var step = CurrentStep;
            if (step == null) return null;
            return ResolveMachineSelector(step.TargetMachine);
        }

        private void TryAdvance(TutorialTrigger any, TutorialTrigger specific, string machineId)
        {
            var step = CurrentStep;
            if (step == null) return;

            var match = step.Trigger == any
                || (step.Trigger == specific
                    && (string.IsNullOrEmpty(step.MachineId) || step.MachineId == machineId));
            if (!match) return;

            _model.SetTutorialStep(_model.TutorialStep + 1);
        }

        private string ResolveMachineSelector(string selector)
        {
            if (string.IsNullOrEmpty(selector) || selector == "first")
                return _config.Machines != null && _config.Machines.Length > 0
                    ? _config.Machines[0].Id
                    : null;

            if (selector == "firstLocked")
            {
                foreach (var m in _model.Machines)
                    if (!m.IsUnlocked) return m.Id;
                return null;
            }

            return selector;
        }
    }
}
