using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Feedbacks;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Managers.Extensions;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Validators;
using Mars.Shared.Common;

namespace Mars.Host.Services;

internal class FeedbackService : IFeedbackService
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IEventManager _eventManager;
    private readonly IExcelService _excelService;
    private readonly IValidatorFabric _validatorFabric;

    public FeedbackService(
        IFeedbackRepository feedbackRepository,
        IEventManager eventManager,
        IExcelService excelService,
        IValidatorFabric validatorFabric)
    {
        _feedbackRepository = feedbackRepository;
        _eventManager = eventManager;
        _excelService = excelService;
        _validatorFabric = validatorFabric;
    }

    public Task<FeedbackSummary?> Get(Guid id, CancellationToken cancellationToken)
        => _feedbackRepository.Get(id, cancellationToken);

    public Task<FeedbackDetail?> GetDetail(Guid id, CancellationToken cancellationToken)
        => _feedbackRepository.GetDetail(id, cancellationToken);

    public Task<ListDataResult<FeedbackSummary>> List(ListFeedbackQuery query, CancellationToken cancellationToken)
        => _feedbackRepository.List(query, cancellationToken);

    public Task<PagingResult<FeedbackSummary>> ListTable(ListFeedbackQuery query, CancellationToken cancellationToken)
        => _feedbackRepository.ListTable(query, cancellationToken);

    public async Task<FeedbackDetail> Create(CreateFeedbackQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);

        var id = await _feedbackRepository.Create(query, cancellationToken);
        var created = await GetDetail(id, cancellationToken);

        //if (created != null)
        {
            ManagerEventPayload payload = new ManagerEventPayload(_eventManager.Defaults.FeedbackAdd(), created);//TODO: сделать явный тип.
            _eventManager.TriggerEvent(payload);
        }

        return created;
    }

    public async Task<FeedbackDetail> Update(UpdateFeedbackQuery query, CancellationToken cancellationToken)
    {
        await _validatorFabric.ValidateAndThrowAsync(query, cancellationToken);

        await _feedbackRepository.Update(query, cancellationToken);
        var updated = await GetDetail(query.Id, cancellationToken);

        ManagerEventPayload payload = new ManagerEventPayload(_eventManager.Defaults.FeedbackUpdate(), updated);
        _eventManager.TriggerEvent(payload);

        return updated;
    }

    public async Task<UserActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var post = await Get(id, cancellationToken) ?? throw new NotFoundException();

        try
        {
            await _feedbackRepository.Delete(id, cancellationToken);

            //if (result.Ok)
            {
                ManagerEventPayload payload = new ManagerEventPayload(_eventManager.Defaults.FeedbackDelete(), post);
                _eventManager.TriggerEvent(payload);
            }
            return UserActionResult.Success();
        }
        catch (Exception ex)
        {
            return UserActionResult.Exception(ex);
        }
    }

    public async Task ExcelFeedbackList(MemoryStream stream, CancellationToken cancellationToken)
    {
        string templateFile = Path.Join("Res", "Excel", "Feedback", "feedback_list.xlsx");

        var feedbackList = await _feedbackRepository.ListAllDetail(new(), cancellationToken);

        _excelService.BuildExcelReport(templateFile, new FeedbacksExcelReportDto(feedbackList), stream);
    }

    internal class FeedbacksExcelReportDto
    {
        public string Title { get; set; } = "Письма";

        public IReadOnlyCollection<FeedbackDetail> Feedbacks { get; set; }

        public FeedbacksExcelReportDto(IReadOnlyCollection<FeedbackDetail> list)
        {
            this.Feedbacks = list;
        }
    }
}
