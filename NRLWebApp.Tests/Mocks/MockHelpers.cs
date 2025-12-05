using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace NRLWebApp.Tests.Mocks
{
    public static class MockHelpers
    {
        public static Mock<UserManager<TUser>> MockUserManager<TUser>(List<TUser> ls) where TUser : class
        {
            var store = new Mock<IUserStore<TUser>>();

            // Create non-null dependencies required by UserManager constructor
            var options = new Mock<IOptions<IdentityOptions>>().Object;
            var passwordHasher = new Mock<IPasswordHasher<TUser>>().Object;
            var userValidators = new List<IUserValidator<TUser>>();
            var passwordValidators = new List<IPasswordValidator<TUser>>();
            var keyNormalizer = new Mock<ILookupNormalizer>().Object;
            var errors = new IdentityErrorDescriber();
            var services = new Mock<IServiceProvider>().Object;
            var logger = new Mock<ILogger<UserManager<TUser>>>().Object;

            var mgr = new Mock<UserManager<TUser>>(
                store.Object,
                options,
                passwordHasher,
                userValidators,
                passwordValidators,
                keyNormalizer,
                errors,
                services,
                logger);

            mgr.Object.UserValidators.Add(new UserValidator<TUser>());
            mgr.Object.PasswordValidators.Add(new PasswordValidator<TUser>());

            // VIKTIG: Bruk TestAsyncEnumerable her for å støtte CountAsync o.l.
            var queryable = new TestAsyncEnumerable<TUser>(ls);

            mgr.Setup(x => x.Users).Returns(queryable);
            mgr.Setup(x => x.DeleteAsync(It.IsAny<TUser>())).ReturnsAsync(IdentityResult.Success);
            mgr.Setup(x => x.CreateAsync(It.IsAny<TUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            mgr.Setup(x => x.UpdateAsync(It.IsAny<TUser>())).ReturnsAsync(IdentityResult.Success);
            mgr.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
               .ReturnsAsync((string id) => ls.FirstOrDefault(u => u.ToString() == id));

            return mgr;
        }

        public static Mock<RoleManager<IdentityRole>> MockRoleManager(List<IdentityRole>? roles = null)
        {
            roles ??= new List<IdentityRole>();
            var store = new Mock<IRoleStore<IdentityRole>>();

            // Create non-null dependencies required by RoleManager constructor
            var roleValidators = new List<IRoleValidator<IdentityRole>>();
            var keyNormalizer = new Mock<ILookupNormalizer>().Object;
            var errors = new IdentityErrorDescriber();
            var logger = new Mock<ILogger<RoleManager<IdentityRole>>>().Object;

            var mgr = new Mock<RoleManager<IdentityRole>>(store.Object, roleValidators, keyNormalizer, errors, logger);

            // Også her, bruk AsyncEnumerable hvis du gjør spørringer mot roller
            var queryable = new TestAsyncEnumerable<IdentityRole>(roles);
            mgr.Setup(x => x.Roles).Returns(queryable);
            mgr.Setup(x => x.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            return mgr;
        }
    }

    // --- HJELPEKLASSER FOR ASYNC MOCKING ---

    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            // _inner.Execute may return null at runtime; use null-forgiving since callers expect non-nullable
            return _inner.Execute(expression)!;
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression)!;
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            // This implementation mirrors typical EF Core testing helpers:
            // build a Task.FromResult(...) instance for the generic result.
            var expectedResultType = typeof(TResult).GetGenericArguments().FirstOrDefault() ?? typeof(object);

            var executeMethod = typeof(IQueryProvider).GetMethods()
                .First(m => m.Name == nameof(IQueryProvider.Execute) && m.IsGenericMethod && m.GetParameters().Length == 1);

            var executionResult = executeMethod
                .MakeGenericMethod(expectedResultType)
                .Invoke(_inner, new object[] { expression })!;

            var fromResultMethod = typeof(Task).GetMethods()
                .First(m => m.Name == nameof(Task.FromResult) && m.IsGenericMethod);

            var taskObj = fromResultMethod
                .MakeGenericMethod(expectedResultType)
                .Invoke(null, new[] { executionResult })!;

            return (TResult)taskObj;
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerable(Expression expression) : base(expression) { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        public TestAsyncEnumerator(IEnumerator<T> inner) { _inner = inner; }
        public ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
        public ValueTask<bool> MoveNextAsync() { return ValueTask.FromResult(_inner.MoveNext()); }
        public T Current => _inner.Current!;
    }
}