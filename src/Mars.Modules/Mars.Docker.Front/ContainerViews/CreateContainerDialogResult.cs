using Mars.Docker.Contracts;

namespace Mars.Docker.Front.ContainerViews;

public record CreateContainerDialogResult(CreateContainerRequest Request, bool StartAfterCreate);
