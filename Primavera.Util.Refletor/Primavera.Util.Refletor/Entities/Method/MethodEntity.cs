using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primavera.Util.Refletor.Entities
{
    public class MethodEntity
    {
        public string Name { get; set; }
        public bool IsPublic { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsStatic { get; set; }
        public bool IsConstructor { get; set; }
        public bool HasExceptionHandlers { get; set; }
        public TypeReference ReturnType { get; set; }
        public string ReturnTypeName { get; set; }
        public List<MethodParameter> Parameters { get; set; }
        public MethodLocation MethodLocation { get; set; }
        public List<MethodVariable> Variables { get; }
        public List<MethodException> Exceptions { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodEntity"/> class.
        /// </summary>
        public MethodEntity()
        {
            this.Parameters = new List<MethodParameter>();
            this.Variables = new List<MethodVariable>();
            this.Exceptions = new List<MethodException>();
        }

        /// <summary>
        /// Sets the method declaration.
        /// </summary>
        /// <param name="method">The method.</param>
        public void SetMethodDeclaration(MethodDefinition method)
        {
            this.IsPublic = method.IsPublic;
            this.IsStatic = method.IsStatic;
            this.IsAbstract = method.IsAbstract;
            this.IsConstructor = method.IsConstructor;
            this.Name = method.Name;
            this.ReturnType = method.MethodReturnType.ReturnType;
            this.ReturnTypeName = GetSystemTypeName(method.MethodReturnType.ReturnType).Replace("`1", "");
            if (method.Body != null)
            {
                this.HasExceptionHandlers = method.Body.HasExceptionHandlers;
            }

            if (this.HasExceptionHandlers)
            {
                foreach(ExceptionHandler exceptionHandler in method.Body.ExceptionHandlers)
                {
                    if(exceptionHandler.CatchType != null)
                    { 
                        if(exceptionHandler.CatchType.FullName == typeof(Exception).FullName)
                        {
                            MethodException methodException = new MethodException();
                            methodException.TryStart = exceptionHandler.TryStart;
                            methodException.TryEnd = exceptionHandler.TryEnd;
                            this.Exceptions.Add(methodException);
                        }
                    }
                }
            }

            if (method.Body!= null)
            { 
                foreach (var variable in method.Body.Variables)
                {
                    if (!string.IsNullOrEmpty(variable.Name))
                    {
                        MethodVariable methodVariable = new MethodVariable();
                        methodVariable.Type = variable.VariableType.Name;
                        methodVariable.Name = GetVariableName(variable);
                        this.Variables.Add(methodVariable);
                    }
                }
            }

            if (method.HasParameters)
            {
                this.Parameters = new List<MethodParameter>();
                foreach (var parameter in method.Parameters)
                {
                    MethodParameter methodParameter = new MethodParameter();
                    methodParameter.Name = parameter.Name;
                    methodParameter.ParameterType = parameter.ParameterType;
                    methodParameter.ParameterTypeName = GetSystemTypeName(parameter.ParameterType);

                    this.Parameters.Add(methodParameter);
                }
            }

            this.MethodLocation = this.GetLocation(method);
        }

        /// <summary>
        /// Gets the name of the system type.
        /// </summary>
        /// <param name="reference">The reference.</param>
        /// <returns></returns>
        public string GetSystemTypeName(TypeReference reference)
        {
            // A quick and dirty fix
            if (reference.Name == "Void")
                return "void";
            if (reference.IsValueType)
            {
                var output = reference.Name.ToLower();
                if (output == "int16") return "short";
                if (output == "int32") return "int";
                if (output == "int64") return "long";
                return output;
            }
            return reference.Name;
        }

        /// <summary>
        /// Gets the location.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns></returns>
        private MethodLocation GetLocation(MethodDefinition method)
        {
            if(method.Body != null)
            { 
                foreach (var instruction in method.Body.Instructions)
                {
                    if (instruction.SequencePoint != null)
                    {
                        return new MethodLocation
                        {
                            Url = instruction.SequencePoint.Document.Url,
                            Line = instruction.SequencePoint.StartLine
                        };
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// This Function will return simple method body, translating CIL into C#.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="indentStart"></param>
        /// <returns></returns>
        public string GenerateMethodBody(MethodDefinition method, int indentStart)
        {
            var output = "";

            // If we have any variables declared within the scope of the Method
            // We should add them to the start of the body.
            // This is just a simple approach for now, to make it more readable.
            foreach (var variable in method.Body.Variables)
            {
                if (!string.IsNullOrEmpty(variable.Name))
                {
                    output += Indent(variable.VariableType.Name + " " + GetVariableName(variable) + ";", indentStart) + Environment.NewLine;
                }
            }


            // Loop through our instructions as before
            // But this time, we actually want to do something with 
            // the instructions we have.
            foreach (var instruction in method.Body.Instructions)
            {
                var opcode = instruction.OpCode;

                // Check if the instruction is for 
                // storing a value onto a variable
                if (InstructionHelper.IsStore(opcode.Code))
                {
                    output += Indent(instruction.Operand + " = " + GetValueOf(instruction.Previous) + ";", indentStart) + Environment.NewLine;
                }
                // Check if we want to return the method
                // And if there is any value we want to return.
                else if (opcode.Code == Code.Ret)
                {
                    if (InstructionHelper.IsLoad(instruction.Previous.OpCode.Code))
                        output += Indent("return " + GetValueOf(instruction.Previous) + ";", indentStart) + Environment.NewLine;
                    else
                        output += Indent("return;", indentStart) + Environment.NewLine;
                }
                else
                {
                    // Print out any unhandled instructions for us to view.
                    output += Indent("// {0}: {1} {2}", indentStart, instruction.Offset, instruction.OpCode,
                        instruction.Operand) + Environment.NewLine;
                }

            }

            return output;
        }

        /// <summary>
        /// This function will try and return a variable name
        /// if one exists, if it doesnt. It will generate one using the typename
        /// and variable index.
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        public string GetVariableName(VariableDefinition variable)
        {
            if (string.IsNullOrEmpty(variable.Name))
            {
                return (variable.VariableType.Name[0] + "" + variable.Index).ToLower();
            }
            return variable.Name;
        }

        public string Indent(string inputFormat, int numberOfIndents, params object[] vars)
        {
            var output = inputFormat;
            for (var i = 0; i < numberOfIndents; i++)
            {
                output = "   " + output;
            }
            if (vars.Length > 0)
                return string.Format(output, vars);
            return output;
        }

        /// <summary>
        /// This function will return a string, representing the values grabbed from the instruction
        /// </summary>
        /// <param name="instruction"></param>
        /// <returns></returns>
        public string GetValueOf(Instruction instruction)
        {
            var code = instruction.OpCode.Code;
            // Check if we are trying to load a value
            // Can be, Load a field, load a constant value, argument
            // variable and more.
            if (InstructionHelper.IsLoad(code))
            {

                // Check if we want to return a null value
                if (InstructionHelper.IsLoadNull(instruction.OpCode.Code))
                    return "null";

                // For now, we will settle with just handling integers.
                // We will have to add more if checks later to support
                // strings, variables, etc.
                if (InstructionHelper.IsLoadInteger(code))
                {
                    // In case the integer is bigger than 4
                    // We will need to get the value from the
                    // operand instead of the "index".
                    if (InstructionHelper.IsLoadN(code))
                    {
                        return instruction.Operand.ToString();
                    }
                    else
                    {
                        // The GetCodeIndex will return the value used from
                        // the constant load. For instance ldc.i4.4 will return
                        // the value 4, ldc.i4.3 will return value 3. Etc.
                        return InstructionHelper.GetCodeIndex(code).ToString();
                    }
                }
            }

            // If our instructions are to call a method
            if (InstructionHelper.IsCallMethod(code))
            {
                // We will need to grab the information of that specific method
                // By casting the Operand into a MethodDefinition.
                var callingMethod = instruction.Operand as MethodDefinition;
                // Note: We are not managing any parameters yet.
                if (callingMethod != null)
                {
                    return callingMethod.Name + "()";
                }
            }
            // Return a empty value if we can't determine what we want to return.
            return "";
        }
    }
}
