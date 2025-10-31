using MQR.Services.MainframeAction.Sessions.Abstractions;
using MQR.Services.MainframeAction.Sessions.Models;
using Open3270.Interfaces;

namespace MQR.Services.MainframeAction.Sessions.Services;

public sealed class SessionEmulatorServicesFactory : ISessionEmulatorServicesFactory
{
    public SessionEmulatorServices Create(ITnEmulator emulator)
    {
        // Base services around the shared emulator
        var screenDataExtractor = new ScreenDataExtractor(emulator);
        var screenWaiter = new ScreenWaiter(emulator, screenDataExtractor);
        var screenInputWriter = new ScreenInputWriter(emulator, screenDataExtractor);
        var navigationExecutor = new NavigationExecutor(emulator);
        var instructionProcessor = new InstructionProcessor(emulator, screenWaiter, screenInputWriter, navigationExecutor, screenDataExtractor);
        var successEvaluator = new SuccessConditionEvaluator(emulator, screenWaiter);
        var availabilityChecker = new ScreenAvailabilityChecker(emulator, screenWaiter, screenDataExtractor);
        var emulatorGate = new SemaphoreEmulatorGate();

        return new SessionEmulatorServices
        {
            LogonRunner = new LogonRunner(emulator, screenWaiter, instructionProcessor, successEvaluator),
            QueryRunner = new QueryRunner(emulator, instructionProcessor),
            ScreenWaiter = screenWaiter,
            ScreenInputWriter = screenInputWriter,
            NavigationExecutor = navigationExecutor,
            ScreenDataExtractor = screenDataExtractor,
            InstructionProcessor = instructionProcessor,
            SuccessConditionEvaluator = successEvaluator,
            ScreenAvailabilityChecker = availabilityChecker,
            EmulatorGate = emulatorGate
        };
    }
}
