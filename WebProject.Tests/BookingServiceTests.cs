using Moq;
using System.Linq.Expressions;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _mockRepository;
    private readonly Mock<IEventService> _mockEventService;
    private readonly IBookingService _service;

    public BookingServiceTests()
    {
        _mockRepository = new Mock<IBookingRepository>();
        _mockEventService = new Mock<IEventService>();
        _service = new BookingService(_mockRepository.Object, _mockEventService.Object);
    }
    //Успешные сценарии:

    //создание брони для существующего события — возвращается BookingInfo со статусом Pending;

    //создание нескольких броней для одного события — все создаются с уникальными Id;
    //получение брони по Id — возвращается корректная информация;
    //получение брони отражает изменение статуса(после Confirm/Reject).
    //Неуспешные сценарии:
    //создание брони для несуществующего события;
    //создание брони для удалённого события;
    //получение брони по несуществующему Id.
}