using TaskFlow.Application.Common.Pagination;

namespace TaskFlow.Application.Tests.Common.Pagination;

public class PagingLinkBuilderTests
{
    [Fact]
    public void Build_OnFirstPageWithMorePages_ReturnsNullPrevAndNonNullNext()
    {
        var (prev, next) = PagingLinkBuilder.Build(page: 1, perPage: 5, total: 12, status: null);

        Assert.Null(prev);
        Assert.Equal("/api/tasks?page=2&perPage=5", next);
    }

    [Fact]
    public void Build_OnMiddlePage_ReturnsBothPrevAndNext()
    {
        var (prev, next) = PagingLinkBuilder.Build(page: 2, perPage: 5, total: 12, status: null);

        Assert.Equal("/api/tasks?page=1&perPage=5", prev);
        Assert.Equal("/api/tasks?page=3&perPage=5", next);
    }

    [Fact]
    public void Build_OnLastPage_ReturnsNonNullPrevAndNullNext()
    {
        var (prev, next) = PagingLinkBuilder.Build(page: 3, perPage: 5, total: 12, status: null);

        Assert.Equal("/api/tasks?page=2&perPage=5", prev);
        Assert.Null(next);
    }

    [Fact]
    public void Build_WithStatusFilter_PreservesStatusOnBothLinksInCanonicalOrder()
    {
        var (prev, next) = PagingLinkBuilder.Build(page: 2, perPage: 5, total: 15, status: "Pending");

        Assert.Equal("/api/tasks?page=1&perPage=5&status=Pending", prev);
        Assert.Equal("/api/tasks?page=3&perPage=5&status=Pending", next);
    }

    [Fact]
    public void Build_WithStatusContainingSpace_EscapesSpaceInLink()
    {
        var (_, next) = PagingLinkBuilder.Build(page: 1, perPage: 5, total: 12, status: "In Progress");

        Assert.Equal("/api/tasks?page=2&perPage=5&status=In%20Progress", next);
    }

    [Fact]
    public void Build_WhenPageBeyondTotal_ReturnsPrevAndNullNext()
    {
        var (prev, next) = PagingLinkBuilder.Build(page: 99, perPage: 10, total: 12, status: null);

        Assert.Equal("/api/tasks?page=98&perPage=10", prev);
        Assert.Null(next);
    }

    [Fact]
    public void Build_WhenTotalIsZero_ReturnsNullPrevAndNullNext()
    {
        var (prev, next) = PagingLinkBuilder.Build(page: 1, perPage: 20, total: 0, status: null);

        Assert.Null(prev);
        Assert.Null(next);
    }

    [Fact]
    public void Build_WhenExactlyOnePageOfResults_ReturnsNullPrevAndNullNext()
    {
        var (prev, next) = PagingLinkBuilder.Build(page: 1, perPage: 20, total: 20, status: null);

        Assert.Null(prev);
        Assert.Null(next);
    }
}
