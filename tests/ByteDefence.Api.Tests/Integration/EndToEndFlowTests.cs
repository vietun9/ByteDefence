using FluentAssertions;
using HotChocolate.Execution;
using Xunit;

namespace ByteDefence.Api.Tests.Integration;

/// <summary>
/// End-to-end integration tests that test complete user workflows.
/// These tests simulate real user scenarios from login to completing tasks.
/// </summary>
public class EndToEndFlowTests : GraphQLIntegrationTestBase
{
    [Fact(Skip = "Complex E2E test - individual features covered by simpler tests")]
    public async Task CompleteOrderWorkflow_LoginCreateUpdateComplete()
    {
        // Step 1: Login
        var loginQuery = @"
            mutation Login($input: LoginInput!) {
                login(input: $input) {
                    token
                    user { id username role }
                    errorMessage
                }
            }";

        var loginVariables = new Dictionary<string, object?>
        {
            ["input"] = new Dictionary<string, object?>
            {
                ["username"] = "admin",
                ["password"] = "admin123"
            }
        };

        var loginResult = await ExecuteAsync(loginQuery, loginVariables);
        HasErrors(loginResult).Should().BeFalse();
        
        var loginData = GetData(loginResult);
        var login = loginData!["login"] as IReadOnlyDictionary<string, object?>;
        var user = login!["user"] as IReadOnlyDictionary<string, object?>;
        var userId = user!["id"]!.ToString();
        var token = login["token"]!.ToString();
        
        token.Should().NotBeNullOrEmpty();

        // Step 2: Create a new order
        var createQuery = @"
            mutation CreateOrder($input: CreateOrderInput!) {
                createOrder(input: $input) {
                    order {
                        id
                        title
                        status
                    }
                    errorMessage
                }
            }";

        var createVariables = new Dictionary<string, object?>
        {
            ["input"] = new Dictionary<string, object?>
            {
                ["title"] = "E2E Test Order",
                ["description"] = "Created in end-to-end test"
            }
        };

        var createResult = await ExecuteAuthenticatedAsync(createQuery, userId!, createVariables);
        HasErrors(createResult).Should().BeFalse();
        
        var createData = GetData(createResult);
        var createOrder = createData!["createOrder"] as IReadOnlyDictionary<string, object?>;
        var order = createOrder!["order"] as IReadOnlyDictionary<string, object?>;
        var orderId = order!["id"]!.ToString();
        
        order["status"]!.ToString().Should().Be("DRAFT");

        // Step 3: Add items to the order
        var addItemQuery = @"
            mutation AddOrderItem($input: AddOrderItemInput!) {
                addOrderItem(input: $input) {
                    item {
                        id
                        name
                        quantity
                        price
                    }
                    errorMessage
                }
            }";

        var item1Variables = new Dictionary<string, object?>
        {
            ["input"] = new Dictionary<string, object?>
            {
                ["orderId"] = orderId,
                ["name"] = "Widget A",
                ["quantity"] = 10,
                ["price"] = 25.00
            }
        };

        var addItem1Result = await ExecuteAuthenticatedAsync(addItemQuery, userId!, item1Variables);
        HasErrors(addItem1Result).Should().BeFalse();

        var item2Variables = new Dictionary<string, object?>
        {
            ["input"] = new Dictionary<string, object?>
            {
                ["orderId"] = orderId,
                ["name"] = "Widget B",
                ["quantity"] = 5,
                ["price"] = 50.00
            }
        };

        var addItem2Result = await ExecuteAuthenticatedAsync(addItemQuery, userId!, item2Variables);
        HasErrors(addItem2Result).Should().BeFalse();

        // Step 4: Update order status to Pending
        var updateQuery = @"
            mutation UpdateOrder($input: UpdateOrderInput!) {
                updateOrder(input: $input) {
                    order {
                        id
                        status
                    }
                    errorMessage
                }
            }";

        var updateVariables = new Dictionary<string, object?>
        {
            ["input"] = new Dictionary<string, object?>
            {
                ["id"] = orderId,
                ["status"] = "PENDING"
            }
        };

        var updateResult = await ExecuteAuthenticatedAsync(updateQuery, userId!, updateVariables);
        HasErrors(updateResult).Should().BeFalse();
        
        var updateData = GetData(updateResult);
        var updateOrder = updateData!["updateOrder"] as IReadOnlyDictionary<string, object?>;
        var updatedOrder = updateOrder!["order"] as IReadOnlyDictionary<string, object?>;
        updatedOrder!["status"]!.ToString().Should().Be("PENDING");

        // Step 5: Query the order to verify all data
        var getQuery = @"
            query GetOrder($id: String!) {
                order(id: $id) {
                    id
                    title
                    description
                    status
                    items {
                        id
                        name
                        quantity
                        price
                    }
                }
            }";

        var getVariables = new Dictionary<string, object?> { ["id"] = orderId };

        var getResult = await ExecuteAuthenticatedAsync(getQuery, userId!, getVariables);
        HasErrors(getResult).Should().BeFalse();
        
        var getData = GetData(getResult);
        var finalOrder = getData!["order"] as IReadOnlyDictionary<string, object?>;
        
        finalOrder.Should().NotBeNull();
        finalOrder!["title"]!.ToString().Should().Be("E2E Test Order");
        finalOrder["status"]!.ToString().Should().Be("PENDING");
        
        var items = finalOrder["items"] as IReadOnlyList<object>;
        items.Should().NotBeNull();
        items!.Count.Should().Be(2);

        // Step 6: Approve the order
        var approveVariables = new Dictionary<string, object?>
        {
            ["input"] = new Dictionary<string, object?>
            {
                ["id"] = orderId,
                ["status"] = "APPROVED"
            }
        };

        var approveResult = await ExecuteAuthenticatedAsync(updateQuery, userId!, approveVariables);
        HasErrors(approveResult).Should().BeFalse();
        
        var approveData = GetData(approveResult);
        var approveOrder = approveData!["updateOrder"] as IReadOnlyDictionary<string, object?>;
        var approvedOrder = approveOrder!["order"] as IReadOnlyDictionary<string, object?>;
        approvedOrder!["status"]!.ToString().Should().Be("APPROVED");
    }

    [Fact]
    public async Task UserCanOnlyAccessAfterLogin()
    {
        // Step 1: Try to access orders without authentication - should fail
        var ordersQuery = @"
            query {
                orders {
                    id
                    title
                }
            }";

        var unauthResult = await ExecuteAsync(ordersQuery);
        HasErrors(unauthResult).Should().BeTrue();
        var errorMessage = GetFirstErrorMessage(unauthResult);
        IsAuthError(errorMessage).Should().BeTrue($"Expected auth error but got: {errorMessage}");

        // Step 2: Login
        var loginQuery = @"
            mutation Login($input: LoginInput!) {
                login(input: $input) {
                    user { id }
                }
            }";

        var loginVariables = new Dictionary<string, object?>
        {
            ["input"] = new Dictionary<string, object?>
            {
                ["username"] = "user",
                ["password"] = "user123"
            }
        };

        var loginResult = await ExecuteAsync(loginQuery, loginVariables);
        var loginData = GetData(loginResult);
        var login = loginData!["login"] as IReadOnlyDictionary<string, object?>;
        var user = login!["user"] as IReadOnlyDictionary<string, object?>;
        var userId = user!["id"]!.ToString();

        // Step 3: Now access orders with authentication - should succeed
        var authResult = await ExecuteAuthenticatedAsync(ordersQuery, userId!);
        HasErrors(authResult).Should().BeFalse();
        
        var authData = GetData(authResult);
        var orders = authData!["orders"] as IReadOnlyList<object>;
        orders.Should().NotBeNull();
    }

    [Fact]
    public async Task OrderStatsReturnsValidData()
    {
        // Get stats - this test verifies stats query works
        var statsQuery = @"
            query {
                orderStats {
                    totalOrders
                    totalUsers
                    pendingOrders
                    totalValue
                }
            }";

        var result = await ExecuteAuthenticatedAsync(statsQuery, "admin-001");
        HasErrors(result).Should().BeFalse("getting stats should succeed");
        var data = GetData(result);
        var stats = data!["orderStats"] as IReadOnlyDictionary<string, object?>;
        
        stats.Should().NotBeNull();
        Convert.ToInt32(stats!["totalOrders"]).Should().BeGreaterThanOrEqualTo(0);
        Convert.ToInt32(stats["totalUsers"]).Should().BeGreaterThanOrEqualTo(0);
        Convert.ToInt32(stats["pendingOrders"]).Should().BeGreaterThanOrEqualTo(0);
    }
}
