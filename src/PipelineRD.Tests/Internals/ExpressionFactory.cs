using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace PipelineRD.Test.Internals
{
    internal static class ExpressionFactory
    {
        private static readonly MethodInfo getterContextPropertyMethodInfo = typeof(ContextSample).GetProperty(nameof(ContextSample.CreateUserApiFourHandlerWasExecuted)).GetGetMethod();
        private static readonly MethodInfo getterRequestDocumentNumberPropertyMethodInfo = typeof(SampleRequest).GetProperty(nameof(SampleRequest.Number)).GetGetMethod();
        private static readonly MethodInfo getterRequestNamePropertyMethodInfo = typeof(SampleRequest).GetProperty(nameof(SampleRequest.Name)).GetGetMethod();
        private static readonly MethodInfo getterStringLengthMethodInfo = typeof(string).GetProperty(nameof(string.Length)).GetGetMethod();
        private static readonly PropertyInfo stringCharIndexPropertyInfo = typeof(string).GetProperty("Chars");
        
        private static readonly MethodInfo stringGetCharPropertyInfo = typeof(string).GetMethod("get_Chars");

        private static readonly ConstructorInfo argumentExceptionConstructorInfo = typeof(ArgumentException).GetConstructor(new Type[] { typeof(string) });
        private static readonly MethodInfo charEqualsMethodInfo = typeof(char).GetMethod(nameof(char.Equals), new Type[] { typeof(char) });

        public static Expression ComplexExpression()
        {
            var contextParameter = Parameter(typeof(ContextSample), "ctx");
            var requestParameter = Parameter(typeof(SampleRequest), "req");
            var iParameter = Parameter(typeof(int), "i");
            var nameParameter = Parameter(typeof(string), "name");
            var nameLengthParameter = Parameter(typeof(int), "nameLength");
            var nameCharParameter = Parameter(typeof(char), "nameChar");

            var endOfLoopLabel = Label();

            var expression = Block(
                TryCatch(
                    Block(
                        IfThen(
                            IsTrue(
                                Call(contextParameter, getterContextPropertyMethodInfo)),
                             Block(
                                Switch(
                                    Call(requestParameter, getterRequestDocumentNumberPropertyMethodInfo),
                                    SwitchCase(
                                        Throw(
                                            New(
                                                argumentExceptionConstructorInfo,
                                                Constant($"Failure, DocumentNumber is 0"))),
                                        Constant(0)),
                                    SwitchCase(
                                        Block(
                                            new[] { nameParameter, iParameter, nameLengthParameter },
                                            Assign(
                                                nameParameter,
                                                Call(
                                                    requestParameter,
                                                    getterRequestNamePropertyMethodInfo)),
                                            Assign(
                                                iParameter,
                                                Constant(0)),
                                            Assign(
                                                nameLengthParameter,
                                                Call(
                                                    nameParameter,
                                                    getterStringLengthMethodInfo)),
                                            Loop(
                                                IfThenElse(
                                                    LessThan(iParameter, nameLengthParameter),
                                                    Block(
                                                        new[] { nameCharParameter },
                                                        Assign(
                                                            nameCharParameter,
                                                            MakeIndex(
                                                                nameParameter, 
                                                                stringCharIndexPropertyInfo, 
                                                                new Expression[] { iParameter })),
                                                        IfThen(
                                                            Call(
                                                                nameCharParameter,
                                                                charEqualsMethodInfo,
                                                                Constant('s')),
                                                            Throw(
                                                                New(
                                                                    argumentExceptionConstructorInfo, 
                                                                    Constant($"Failure, DocumentNumber is 1 and Name contains 's'")))),
                                                        Assign(
                                                            iParameter,
                                                            Increment(iParameter))),
                                                    Break(endOfLoopLabel)),
                                                endOfLoopLabel)),
                                        Constant(1))))),
                        Constant(true)
                    ),
                    Catch(
                        typeof(ArgumentException),
                        Constant(false)
                    )
                ));

            var lambdaExpression = Lambda<Func<ContextSample, SampleRequest, bool>>(expression, "ComplexMethod", new[] { contextParameter, requestParameter });

            return lambdaExpression;
        }

        public static Expression SimpleExpression(string exceptionMessage)
        {
            MethodInfo getterMethodInfo = typeof(ContextSample).GetProperty("CreateUserApiFourHandlerWasExecuted").GetGetMethod();

            var contextParameter = Parameter(typeof(ContextSample), "ctx");
            var requestParameter = Parameter(typeof(SampleRequest), "req");

            var expression = Block(
                TryCatch(
                    Block(
                        IfThen(
                            IsTrue(Call(contextParameter, getterMethodInfo)),
                            Throw(
                                New(
                                    typeof(ArgumentException).GetConstructor(new Type[] { typeof(string) }), 
                                    Constant(exceptionMessage)))),
                        Constant(true)
                    ),
                    Catch(
                        typeof(ArgumentException),
                        Constant(false)
                    )
                ));

            var lambdaExpression = Lambda<Func<ContextSample, SampleRequest, bool>>(expression, new[] { contextParameter, requestParameter });

            return lambdaExpression;
        }
    }

    public class ContextSample : BaseContext
    {
        public IEnumerable<string> Values { get; set; }

        public bool InitializeCreateUserHandlerWasExecuted { get; set; }
        public bool CreateUserApiOneHandlerWasExecuted { get; set; }
        public bool CreateUserApiTwoHandlerWasExecuted { get; set; }
        public bool UpdateAccountHandlerWasExecuted { get; set; }
        public bool CreateUserApiThreeHandlerWasExecuted { get; set; }
        public bool CreateUserApiFourHandlerWasExecuted { get; set; }
        public bool InitializeCreateUserRecoveryHandlerWasExecuted { get; set; }
        public bool UpdateAccountRecoveryHandlerWasExecuted { get; set; }
        public bool CreateUserApiFourRecoveryHandlerWasExecuted { get; set; }

        public bool InitializeCreateUserRecoveryHandlerShouldAbort { get; set; }
    }

    public class SampleRequest
    {
        public string Name { get; set; }
        public string DocumentNumber { get; set; }
        public int Number { get; set; }
    }
}
