using ByteDefence.Api.GraphQL.DataLoaders;
using ByteDefence.Shared.Models;

namespace ByteDefence.Api.GraphQL.Schema.Types;

public class OrderType : ObjectType<Order>
{
    protected override void Configure(IObjectTypeDescriptor<Order> descriptor)
    {
        descriptor.Description("Represents a purchase order in the system.");

        descriptor.Field(o => o.Id)
            .Type<NonNullType<IdType>>()
            .Description("The unique identifier of the order.");

        descriptor.Field(o => o.Title)
            .Type<NonNullType<StringType>>()
            .Description("The title of the order.");

        descriptor.Field(o => o.Description)
            .Type<StringType>()
            .Description("A detailed description of the order.");

        descriptor.Field(o => o.Status)
            .Type<NonNullType<EnumType<OrderStatus>>>()
            .Description("The current status of the order.");

        descriptor.Field(o => o.Items)
            .Type<NonNullType<ListType<NonNullType<ObjectType<OrderItem>>>>>()
            .Description("The line items in this order.");

        descriptor.Field(o => o.CreatedBy)
            .ResolveWith<OrderResolvers>(r => r.GetCreatedByAsync(default!, default!))
            .Description("The user who created this order.");

        descriptor.Field(o => o.Total)
            .Type<NonNullType<DecimalType>>()
            .Description("The total value of the order (calculated from items).");

        descriptor.Field(o => o.CreatedAt)
            .Type<NonNullType<DateTimeType>>()
            .Description("When the order was created.");

        descriptor.Field(o => o.UpdatedAt)
            .Type<NonNullType<DateTimeType>>()
            .Description("When the order was last updated.");

        descriptor.Field(o => o.CreatedById).Ignore();
    }

    private class OrderResolvers
    {
        /// <summary>
        /// Uses DataLoader to batch user lookups and solve N+1 query problem.
        /// </summary>
        public async Task<User?> GetCreatedByAsync(
            [Parent] Order order,
            UserByIdDataLoader userLoader)
        {
            if (order.CreatedBy != null) return order.CreatedBy;
            if (string.IsNullOrEmpty(order.CreatedById)) return null;
            return await userLoader.LoadAsync(order.CreatedById);
        }
    }
}

public class OrderItemType : ObjectType<OrderItem>
{
    protected override void Configure(IObjectTypeDescriptor<OrderItem> descriptor)
    {
        descriptor.Description("Represents a line item within an order.");

        descriptor.Field(i => i.Id)
            .Type<NonNullType<IdType>>()
            .Description("The unique identifier of the item.");

        descriptor.Field(i => i.Name)
            .Type<NonNullType<StringType>>()
            .Description("The name of the item.");

        descriptor.Field(i => i.Quantity)
            .Type<NonNullType<IntType>>()
            .Description("The quantity ordered.");

        descriptor.Field(i => i.Price)
            .Type<NonNullType<DecimalType>>()
            .Description("The price per unit.");

        descriptor.Field("subtotal")
            .Type<NonNullType<DecimalType>>()
            .Resolve(context => 
            {
                var item = context.Parent<OrderItem>();
                return item.Price * item.Quantity;
            })
            .Description("The subtotal for this line item (price × quantity).");

        descriptor.Field(i => i.OrderId).Ignore();
        descriptor.Field(i => i.Order).Ignore();
    }
}

public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Description("Represents a user in the system.");

        descriptor.Field(u => u.Id)
            .Type<NonNullType<IdType>>()
            .Description("The unique identifier of the user.");

        descriptor.Field(u => u.Username)
            .Type<NonNullType<StringType>>()
            .Description("The username.");

        descriptor.Field(u => u.Email)
            .Type<NonNullType<StringType>>()
            .Description("The email address.");

        descriptor.Field(u => u.Role)
            .Type<NonNullType<EnumType<UserRole>>>()
            .Description("The user's role in the system.");

        descriptor.Field(u => u.CreatedAt)
            .Type<NonNullType<DateTimeType>>()
            .Description("When the user account was created.");

        descriptor.Field(u => u.PasswordHash).Ignore();
        descriptor.Field(u => u.Orders).Ignore();
    }
}
