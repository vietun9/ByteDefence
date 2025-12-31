using FluentAssertions;
using HotChocolate.Execution;
using Xunit;

namespace ByteDefence.Api.Tests.Integration;

/// <summary>
/// Integration tests for Order queries.
/// Tests the complete query flow through GraphQL.
/// </summary>
public class OrderQueryIntegrationTests : GraphQLIntegrationTestBase
{
    private const string AdminUserId = "admin-001";
    private const string UserUserId = "user-001";

    [Fact]
    public async Task GetOrders_WithAuthentication_ReturnsOrders()
    {
        // Arrange
        var query = @"
            query {
                orders {
                    id
                    title
                    status
                    description
                }
            }";

        // Act
        var result = await ExecuteAuthenticatedAsync(query, AdminUserId);

        // Assert
        HasErrors(result).Should().BeFalse();
        var data = GetData(result);
        data.Should().NotBeNull();
        
        var orders = data!["orders"] as IReadOnlyList<object>;
        orders.Should().NotBeNull();
        orders!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetOrders_WithoutAuthentication_ReturnsError()
    {
        // Arrange
        var query = @"
            query {
                orders {
                    id
                    title
                }
            }";

        // Act
        var result = await ExecuteAsync(query);

        // Assert
        HasErrors(result).Should().BeTrue();
        var errorMessage = GetFirstErrorMessage(result);
        IsAuthError(errorMessage).Should().BeTrue($"Expected auth error but got: {errorMessage}");
    }

    [Fact]
    public async Task GetOrder_WithValidId_ReturnsOrder()
    {
        // First, create an order to query
        var createQuery = @"
            mutation CreateOrder($input: CreateOrderInput!) {
                createOrder(input: $input) {
                    order { id title description status }
                }
            }";
        var createVars = new Dictionary<string, object?>
        {
            ["input"] = new Dictionary<string, object?>
            {
                ["title"] = "Test Order for Query",
                ["description"] = "Created for GetOrder test"
            }
        };
        var createResult = await ExecuteAuthenticatedAsync(createQuery, AdminUserId, createVars);
        HasErrors(createResult).Should().BeFalse();
        var createData = GetData(createResult);
        var createdOrder = (createData!["createOrder"] as IReadOnlyDictionary<string, object?>)!["order"] as IReadOnlyDictionary<string, object?>;
        var orderId = createdOrder!["id"]!.ToString();

        // Now query the order
        var query = @"
            query GetOrder($id: String!) {
                order(id: $id) {
                    id
                    title
                    description
                    status
                }
            }";

        var variables = new Dictionary<string, object?>
        {
            ["id"] = orderId
        };

        // Act
        var result = await ExecuteAuthenticatedAsync(query, AdminUserId, variables);

        // Assert
        HasErrors(result).Should().BeFalse();
        var data = GetData(result);
        var order = data!["order"] as IReadOnlyDictionary<string, object?>;
        
        order.Should().NotBeNull();
        order!["id"]!.ToString().Should().Be(orderId);
        order["title"]!.ToString().Should().Be("Test Order for Query");
    }

    [Fact]
    public async Task GetOrder_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var query = @"
            query GetOrder($id: String!) {
                order(id: $id) {
                    id
                    title
                }
            }";

        var variables = new Dictionary<string, object?>
        {
            ["id"] = "non-existent-id"
        };

        // Act
        var result = await ExecuteAuthenticatedAsync(query, AdminUserId, variables);

        // Assert
        HasErrors(result).Should().BeFalse();
        var data = GetData(result);
        data!["order"].Should().BeNull();
    }

    [Fact]
    public async Task GetOrderStats_ReturnsAggregatedData()
    {
        // Arrange
        var query = @"
            query {
                orderStats {
                    totalOrders
                    totalUsers
                    pendingOrders
                    totalValue
                }
            }";

        // Act
        var result = await ExecuteAuthenticatedAsync(query, AdminUserId);

        // Assert
        HasErrors(result).Should().BeFalse();
        var data = GetData(result);
        var stats = data!["orderStats"] as IReadOnlyDictionary<string, object?>;
        
        stats.Should().NotBeNull();
        Convert.ToInt32(stats!["totalOrders"]).Should().BeGreaterThan(0);
        Convert.ToInt32(stats["totalUsers"]).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetMe_WithAuthentication_ReturnsCurrentUser()
    {
        // Arrange
        var query = @"
            query {
                me {
                    id
                    username
                    email
                    role
                }
            }";

        // Act
        var result = await ExecuteAuthenticatedAsync(query, AdminUserId);

        // Assert
        HasErrors(result).Should().BeFalse();
        var data = GetData(result);
        var me = data!["me"] as IReadOnlyDictionary<string, object?>;
        
        me.Should().NotBeNull();
        me!["id"]!.ToString().Should().Be(AdminUserId);
        me["username"]!.ToString().Should().Be("admin");
    }
}
