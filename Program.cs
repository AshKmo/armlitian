namespace Armlitian {
	public enum TokenType {
		WHITESPACE,
		BRACKET,
		WORD,
		STRING,
		CHAR,
		INT,
		FLOAT,
		HEX,
		BIN
	}

	public struct Token {
		public TokenType Type;
		public string Content;

		public Token(TokenType type, string content) {
			Type = type;
			Content = content;
		}

		public override string ToString() {
			return $"({Type} {StringRep.Escape(Content)})";
		}
	}

	public interface Element {
	}

	public struct ListElement : Element {
		public List<Element> Content;

		public ListElement(List<Element> content) {
			Content = content;
		}

		public override string ToString() {
			return $"[{string.Join(" ", Content)}]";
		}
	}

	public struct WordElement : Element {
		public string Content;

		public WordElement(string content) {
			Content = content;
		}

		public override string ToString() {
			return Content;
		}
	}

	public struct CharElement : Element {
		public char Content;

		public CharElement(char content) {
			Content = content;
		}

		public override string ToString() {
			return $"'{StringRep.Escape(Content.ToString())}'";
		}
	}

	public struct StringElement : Element {
		public string Content;

		public StringElement(string content) {
			Content = content;
		}

		public override string ToString() {
			return $"\"{StringRep.Escape(Content)}\"";
		}
	}

	public struct IntElement : Element {
		public int Content;

		public IntElement(int content) {
			Content = content;
		}

		public override string ToString() {
			return Content.ToString();
		}
	}

	public struct FloatElement : Element {
		public float Content;

		public FloatElement(float content) {
			Content = content;
		}

		public override string ToString() {
			return Content.ToString();
		}
	}

	public static class StringRep {
		public static string Escape(string s) {
			return s.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
		}
	}

	public struct Field {
		public string Name;
		public Type Type;
		public int Position;

		public Field(string name, Type type, int position) {
			Name = name;
			Type = type;
			Position = position;
		}

		public override string ToString() {
			return $"[{Type} {Name}]";
		}
	}

	public abstract class Type {
		public string? Name { get; set; }

		public abstract int Size { get; }

		public Type(string? name) {
			Name = name;
		}

		public abstract Type Clone();

		public override string ToString() {
			return $"[{Name}:{GetType()}]";
		}

		public static bool AreEqual(Type a, Type b) {
			if (a.Name != null && a.Name == b.Name) {
				return true;
			}

			if (a.GetType() != b.GetType()) {
				return false;
			}

			switch (a) {
				case IntType:
				case FloatType:
				case VoidType:
				case CharType:
					return true;

				case PtrType ptrType:
					return AreEqual(ptrType.ValueType, ((PtrType)b).ValueType);

				case ArrayType arrType:
					return AreEqual(arrType.ItemType, ((ArrayType)b).ItemType) && arrType.Count == ((ArrayType)b).Count;

				case StructType structType:
					{
						if (structType.Fields.Count != ((StructType)b).Fields.Count) return false;

						int i = 0;

						foreach (Field fieldA in structType.Fields.Values) {
							if (!AreEqual(fieldA.Type, ((StructType)b).Fields.Values.ToList()[i].Type)) return false;

							i++;
						}

						return true;
					}

				case UnresolvedPtrValueType:
					throw new Exception("cannot compare unresolved types");
			}

			return false;
		}
	}

	public class VoidType : Type {
		public override int Size { get { return 0; } }

		public VoidType(string? name) : base(name) {
		}

		public override Type Clone() {
			return new VoidType(Name);
		}
	}

	public class IntType : Type {
		public override int Size { get { return 4; } }

		public IntType(string? name) : base(name) {
		}

		public override Type Clone() {
			return new IntType(Name);
		}
	}

	public class FloatType : Type {
		public override int Size { get { return 4; } }

		public FloatType(string? name) : base(name) {
		}

		public override Type Clone() {
			return new FloatType(Name);
		}
	}

	public class CharType : Type {
		public override int Size { get { return 1; } }

		public CharType(string? name) : base(name) {
		}

		public override Type Clone() {
			return new CharType(Name);
		}
	}

	public class UnresolvedPtrValueType : Type {
		public override int Size { get { return 0; } }

		public ListElement ValueType;

		public UnresolvedPtrValueType(string? name, ListElement valueType) : base(name) {
			ValueType = valueType;
		}

		public override Type Clone() {
			return new UnresolvedPtrValueType(Name, ValueType);
		}

		public override string ToString() {
			return $"[{Name}:{GetType()} {ValueType}]";
		}
	}

	public class PtrType : Type {
		public override int Size { get { return 4; } }

		public Type ValueType;

		public PtrType(string? name, Type valueType) : base(name) {
			ValueType = valueType;
		}

		public override Type Clone() {
			return new PtrType(Name, ValueType);
		}

		public override string ToString() {
			return $"[{Name}:{GetType()} [{ValueType.Name}:{ValueType.GetType()} ...]]";
		}
	}

	public class StructType : Type {
		public override int Size { get { return Fields.Values.Aggregate(0, (sum, x) => sum + x.Type.Size); } }

		public Dictionary<string, Field> Fields;

		public StructType(string? name, List<Field> fields) : base(name) {
			Fields = new Dictionary<string, Field>();

			foreach (Field field in fields) {
				Fields[field.Name] = field;
			}
		}

		public override Type Clone() {
			return new StructType(Name, Fields.Values.ToList());
		}

		public override string ToString() {
			return $"[{Name}:{GetType()} [ {string.Join(" ", Fields.Values)} ]]";
		}
	}

	public class ArrayType : Type {
		public override int Size { get { return ItemType.Size * Count; } }

		public Type ItemType;

		public int Count;

		public ArrayType(string? name, Type itemType, int count) : base(name) {
			ItemType = itemType;
			Count = count;
		}

		public override Type Clone() {
			return new ArrayType(Name, ItemType, Count);
		}

		public override string ToString() {
			return $"[{Name}:{GetType()} {ItemType} {Count}]";
		}
	}

	public class Function {
		public string Name { get; set; }

		public Dictionary<string, Field> Parameters = new Dictionary<string, Field>();

		public Type ReturnType;

		public Element Body;

		public int TotalParameterSize { get { return Parameters.Values.Aggregate(0, (sum, x) => sum + x.Type.Size); } }

		public Assembly.Label Label = new Assembly.Label();

		public Function(string name, Type returnType, List<Field> parameters, Element body) {
			Name = name;
			ReturnType = returnType;
			Body = body;

			foreach (Field parameter in parameters) {
				Parameters[parameter.Name] = parameter;
			}
		}

		public override string ToString() {
			return $"[{ReturnType} {Name} [ {string.Join(" ", Parameters.Values)} ] {Body}]";
		}
	}

	namespace Assembly {
		public interface MemoryLocation {
			public string ToAssembly();
		}

		public class SpecialConstant : ConstantValue, MemoryLocation {
			public static SpecialConstant WriteSignedNum = new SpecialConstant(".WriteSignedNum");
			public static SpecialConstant WriteChar = new SpecialConstant(".WriteChar");
			public static SpecialConstant WriteString = new SpecialConstant(".WriteString");
			public static SpecialConstant PixelScreen = new SpecialConstant(".PixelScreen");

			public string AssemblyRep;

			public SpecialConstant(string assemblyRep) {
				AssemblyRep = assemblyRep;
			}

			public string ToAssembly() {
				return AssemblyRep;
			}

			string Value.ToAssembly() {
				return "#" + AssemblyRep;
			}
		}

		public class IndexedMemoryLocation : MemoryLocation {
			public Register A;

			public ConstantValue? ConstB = null;
			public Register? RegisterB = null;

			public bool Add = true;

			public IndexedMemoryLocation(Register a, ConstantValue b, bool doAdd = true) {
				A = a;
				ConstB = b;
				Add = doAdd;
			}

			public IndexedMemoryLocation(Register a, Register b, bool doAdd = true) {
				A = a;
				RegisterB = b;
				Add = doAdd;
			}

			public IndexedMemoryLocation(Register a) {
				A = a;
				ConstB = new Assembly.ConstInt(0);
			}

			public string ToAssembly() {
				string inner = A.ToAssembly();

				if (ConstB != null || RegisterB != null) {
					if (Add) {
						inner += "+";
					} else {
						inner += "-";
					}
				}

				if (ConstB != null) {
					inner += ConstB.ToAssembly();
				} else if (RegisterB != null) {
					inner += RegisterB.ToAssembly();
				}

				return $"[{inner}]";
			}
		}

		public interface Value : Line {
			public new string ToAssembly();
		}

		public class String : Line {
			public string Content;

			public String(string content) {
				Content = content;
			}

			public string ToAssembly() {
				return $".ASCIZ \"{StringRep.Escape(Content)}\"";
			}
		}

		public enum RegisterType {
			PC,
			LR,
			SP,
			R12,
			R11,
			R10,
			R9,
			R8,
			R7,
			R6,
			R5,
			R4,
			R3,
			R2,
			R1,
			R0
		}

		public class Register : Value {
			public static Register PC = new Register(RegisterType.PC);
			public static Register LR = new Register(RegisterType.LR);
			public static Register SP = new Register(RegisterType.SP);
			public static Register R12 = new Register(RegisterType.R12);
			public static Register R11 = new Register(RegisterType.R11);
			public static Register R10 = new Register(RegisterType.R10);
			public static Register R9 = new Register(RegisterType.R9);
			public static Register R8 = new Register(RegisterType.R8);
			public static Register R7 = new Register(RegisterType.R7);
			public static Register R6 = new Register(RegisterType.R6);
			public static Register R5 = new Register(RegisterType.R5);
			public static Register R4 = new Register(RegisterType.R4);
			public static Register R3 = new Register(RegisterType.R3);
			public static Register R2 = new Register(RegisterType.R2);
			public static Register R1 = new Register(RegisterType.R1);
			public static Register R0 = new Register(RegisterType.R0);

			public RegisterType Type;

			public Register(RegisterType type) {
				Type = type;
			}

			public string ToAssembly() {
				string? name = Enum.GetName(typeof(RegisterType), Type);

				if (name == null) {
					throw new Exception("invalid register type " + Type);
				}

				return name;
			}
		}

		public interface ConstantValue : Value {
		}

		public class ConstInt : ConstantValue, MemoryLocation {
			public int Value;

			public ConstInt(int val) {
				Value = val;
			}

			public string ToAssembly() {
				return Value.ToString();
			}

			string Value.ToAssembly() {
				return $"#{Value}";
			}
		}

		public abstract class Directive : Line {
			public abstract string ToAssembly();
		}

		public class ALIGN : Directive {
			public int Factor;

			public ALIGN(int factor) {
				Factor = factor;
			}

			public override string ToAssembly() {
				return ".ALIGN " + Factor;
			}
		}

		public class Label : ConstantValue, Line, MemoryLocation {
			public Guid ID = Guid.NewGuid();

			public Label() {
			}

			public string ToAssembly() {
				return "label__" + ID.ToString("N");
			}

			string MemoryLocation.ToAssembly() {
				return ToAssembly();
			}

			string Value.ToAssembly() {
				return "#" + ((MemoryLocation)this).ToAssembly();
			}

			string Line.ToAssembly() {
				return ((MemoryLocation)this).ToAssembly() + ":";
			}
		}

		public interface Line {
			public abstract string ToAssembly();
		}

		public abstract class Instruction : Line {
			public abstract string ToAssembly();
		}

		public class MOV : Instruction {
			public Register Register;
			public Value Value;

			public MOV(Register register, Value val) {
				Register = register;
				Value = val;
			}

			public override string ToAssembly() {
				return $"MOV {Register.ToAssembly()}, {Value.ToAssembly()}";
			}
		}

		public class LDR : Instruction {
			public Register Register;
			public MemoryLocation MemoryLocation;

			public LDR(Register register, MemoryLocation memoryLocation) {
				Register = register;
				MemoryLocation = memoryLocation;
			}

			public override string ToAssembly() {
				return $"LDR {Register.ToAssembly()}, {MemoryLocation.ToAssembly()}";
			}
		}

		public class LDRB : Instruction {
			public Register Register;
			public MemoryLocation MemoryLocation;

			public LDRB(Register register, MemoryLocation memoryLocation) {
				Register = register;
				MemoryLocation = memoryLocation;
			}

			public override string ToAssembly() {
				return $"LDRB {Register.ToAssembly()}, {MemoryLocation.ToAssembly()}";
			}
		}

		public class STR : Instruction {
			public Register Register;
			public MemoryLocation MemoryLocation;

			public STR(Register register, MemoryLocation memoryLocation) {
				Register = register;
				MemoryLocation = memoryLocation;
			}

			public override string ToAssembly() {
				return $"STR {Register.ToAssembly()}, {MemoryLocation.ToAssembly()}";
			}
		}

		public class STRB : Instruction {
			public Register Register;
			public MemoryLocation MemoryLocation;

			public STRB(Register register, MemoryLocation memoryLocation) {
				Register = register;
				MemoryLocation = memoryLocation;
			}

			public override string ToAssembly() {
				return $"STRB {Register.ToAssembly()}, {MemoryLocation.ToAssembly()}";
			}
		}

		public class ADD : Instruction {
			public Register Subject;
			public Register A;
			public Value B;

			public ADD(Register subject, Register a, Value b) {
				Subject = subject;
				A = a;
				B = b;
			}

			public override string ToAssembly() {
				return $"ADD {Subject.ToAssembly()}, {A.ToAssembly()}, {B.ToAssembly()}";
			}
		}

		public class LSL : Instruction {
			public Register Subject;
			public Register A;
			public Value B;

			public LSL(Register subject, Register a, Value b) {
				Subject = subject;
				A = a;
				B = b;
			}

			public override string ToAssembly() {
				return $"LSL {Subject.ToAssembly()}, {A.ToAssembly()}, {B.ToAssembly()}";
			}
		}

		public class LSR : Instruction {
			public Register Subject;
			public Register A;
			public Value B;

			public LSR(Register subject, Register a, Value b) {
				Subject = subject;
				A = a;
				B = b;
			}

			public override string ToAssembly() {
				return $"LSR {Subject.ToAssembly()}, {A.ToAssembly()}, {B.ToAssembly()}";
			}
		}

		public class AND : Instruction {
			public Register Subject;
			public Register A;
			public Value B;

			public AND(Register subject, Register a, Value b) {
				Subject = subject;
				A = a;
				B = b;
			}

			public override string ToAssembly() {
				return $"AND {Subject.ToAssembly()}, {A.ToAssembly()}, {B.ToAssembly()}";
			}
		}

		public class OR : Instruction {
			public Register Subject;
			public Register A;
			public Value B;

			public OR(Register subject, Register a, Value b) {
				Subject = subject;
				A = a;
				B = b;
			}

			public override string ToAssembly() {
				return $"OR {Subject.ToAssembly()}, {A.ToAssembly()}, {B.ToAssembly()}";
			}
		}

		public class XOR : Instruction {
			public Register Subject;
			public Register A;
			public Value B;

			public XOR(Register subject, Register a, Value b) {
				Subject = subject;
				A = a;
				B = b;
			}

			public override string ToAssembly() {
				return $"XOR {Subject.ToAssembly()}, {A.ToAssembly()}, {B.ToAssembly()}";
			}
		}

		public class SUB : Instruction {
			public Register Subject;
			public Register A;
			public Value B;

			public SUB(Register subject, Register a, Value b) {
				Subject = subject;
				A = a;
				B = b;
			}

			public override string ToAssembly() {
				return $"SUB {Subject.ToAssembly()}, {A.ToAssembly()}, {B.ToAssembly()}";
			}
		}

		public class CMP : Instruction {
			public Register A;
			public Value B;

			public CMP(Register a, Value b) {
				A = a;
				B = b;
			}

			public override string ToAssembly() {
				return $"CMP {A.ToAssembly()}, {B.ToAssembly()}";
			}
		}

		public class BEQ : Instruction {
			public Label JumpLabel;

			public BEQ(Label jumpLabel) {
				JumpLabel = jumpLabel;
			}

			public override string ToAssembly() {
				return $"BEQ {JumpLabel.ToAssembly()}";
			}
		}

		public class BNE : Instruction {
			public Label JumpLabel;

			public BNE(Label jumpLabel) {
				JumpLabel = jumpLabel;
			}

			public override string ToAssembly() {
				return $"BNE {JumpLabel.ToAssembly()}";
			}
		}

		public class BGT : Instruction {
			public Label JumpLabel;

			public BGT(Label jumpLabel) {
				JumpLabel = jumpLabel;
			}

			public override string ToAssembly() {
				return $"BGT {JumpLabel.ToAssembly()}";
			}
		}

		public class BLT : Instruction {
			public Label JumpLabel;

			public BLT(Label jumpLabel) {
				JumpLabel = jumpLabel;
			}

			public override string ToAssembly() {
				return $"BLT {JumpLabel.ToAssembly()}";
			}
		}

		public class RET : Instruction {
			public RET() {
			}

			public override string ToAssembly() {
				return "RET";
			}
		}

		public class HALT : Instruction {
			public HALT() {
			}

			public override string ToAssembly() {
				return "HALT";
			}
		}


		public class BL : Instruction {
			public Label JumpLabel;

			public BL(Label jumpLabel) {
				JumpLabel = jumpLabel;
			}

			public override string ToAssembly() {
				return $"BL {JumpLabel.ToAssembly()}";
			}
		}

		public class B : Instruction {
			public Label JumpLabel;

			public B(Label jumpLabel) {
				JumpLabel = jumpLabel;
			}

			public override string ToAssembly() {
				return $"B {JumpLabel.ToAssembly()}";
			}
		}
	}

	public struct GenericSubroutines {
		public Assembly.Label Copy;
	}

	public static class Compilation {
		private static void ResolvePtrTypes(Dictionary<string, Type> types, Type type) {
			switch (type) {
				case PtrType ptrType:
					if (ptrType.ValueType is UnresolvedPtrValueType upvt) {
						Type? newType = ConstructType(types, upvt.ValueType);

						if (newType == null) {
							throw new Exception("error resolving pointer type");
						}

						ptrType.ValueType = newType;
					}
					break;

				case ArrayType arrType:
					ResolvePtrTypes(types, arrType.ItemType);
					break;

				case StructType structType:
					foreach (Field field in structType.Fields.Values) {
						ResolvePtrTypes(types, field.Type);
					}
					break;
			}
		}

		private static Field? ConstructField(Dictionary<string, Type> types, ListElement fieldData, int position, bool resolveImmediately = true) {
			Type? type = ConstructType(types, (ListElement)fieldData.Content[0], resolveImmediately);

			if (type == null) {
				return null;
			}

			return new Field(((WordElement)fieldData.Content[1]).Content, type, position);
		}

		private static Type? ConstructType(Dictionary<string, Type> types, ListElement typeData, bool resolveImmediately = true, bool noClones = true) {
			string name = ((WordElement)typeData.Content[0]).Content;

			if (types.ContainsKey(name)) {
				if (noClones) {
					return types[name];
				}

				return types[name].Clone();
			}

			switch (name) {
				case "ptr":
					{
						ListElement valueTypeData = (ListElement)typeData.Content[1];

						Type? valueType;

						if (resolveImmediately) {
							valueType = ConstructType(types, valueTypeData, resolveImmediately);

							if (valueType == null) {
								throw new Exception("failed to immediately resolve pointer value type");
							}
						} else {
							valueType = new UnresolvedPtrValueType(null, valueTypeData);
						}

						return new PtrType(null, valueType);
					}

				case "array":
					{
						Type? itemType = ConstructType(types, (ListElement)(typeData.Content[1]), resolveImmediately);

						if (itemType == null) {
							return null;
						}

						return new ArrayType(
								"array",
								itemType,
								((IntElement)typeData.Content[2]).Content
								);
					}

				case "struct":
					{
						List<Field> fields = new List<Field>();

						int position = 0;

						foreach (ListElement fieldData in ((ListElement)typeData.Content[1]).Content) {
							Field? field = ConstructField(types, fieldData, position, resolveImmediately);

							if (field == null) {
								return null;
							}

							fields.Add((Field)field);
							position += SizeToWordBytes(((Field)field).Type.Size);
						}

						return new StructType(null, fields);
					}
			}

			return null;
		}

		private static int SizeToWordBytes(int size) {
			return (int)Math.Ceiling(size / 4.0) * 4;
		}

		private static (List<Assembly.Line> program, List<Assembly.Line> data, Type type) CompileExpression(GenericSubroutines genericSubroutines, Dictionary<string, Type> types, Dictionary<string, Function> functions, Function f, Dictionary<string, Field> variables, Element expression, int memoryStart) {
			List<Assembly.Line> program = new();
			List<Assembly.Line> data = new();

			switch (expression) {
				case ListElement listEl:
					{
						string opName = ((WordElement)listEl.Content[0]).Content;

						switch (opName) {
							case "do":
								{
									ListElement expressionList;

									int newMemoryStart = memoryStart;

									if (listEl.Content.Count == 3) {
										expressionList = (ListElement)listEl.Content[2];

										Dictionary<string, Field> newVariables = new();

										foreach (Field v in variables.Values) {
											newVariables[v.Name] = v;
										}

										foreach (ListElement vEl in ((ListElement)listEl.Content[1]).Content) {
											Field? v = ConstructField(types, vEl, newMemoryStart);

											if (v == null) {
												throw new Exception("failed to construct variable field");
											}

											newVariables[((Field)v).Name] = (Field)v;

											newMemoryStart += SizeToWordBytes(((Field)v).Type.Size);
										}

										variables = newVariables;
									} else {
										expressionList = (ListElement)listEl.Content[1];
									}

									foreach (Element newExpression in expressionList.Content) {
										var (subProgram, subData, _) = CompileExpression(genericSubroutines, types, functions, f, variables, newExpression, newMemoryStart);

										program = program.Concat(subProgram).ToList();
										data = data.Concat(subData).ToList();
									}

									return (program, data, types["void"]);
								}

							case "return":
								{
									if (listEl.Content.Count == 1) {
										if (!Type.AreEqual(f.ReturnType, types["void"])) {
											throw new Exception("cannot use empty return expression inside a function that does not return void");
										}
									} else {
										var (subProgram, subData, valueType) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[1], memoryStart);

										if (!Type.AreEqual(f.ReturnType, valueType)) {
											throw new Exception("return type mismatch");
										}

										program = program.Concat(subProgram).ToList();
										data = data.Concat(subData).ToList();

										program.Add(new Assembly.ADD(Assembly.Register.R0, Assembly.Register.SP, new Assembly.ConstInt(memoryStart)));
										program.Add(new Assembly.ADD(Assembly.Register.R1, Assembly.Register.SP, new Assembly.ConstInt(0)));
										program.Add(new Assembly.MOV(Assembly.Register.R2, new Assembly.ConstInt(f.ReturnType.Size)));
										program.Add(new Assembly.BL(genericSubroutines.Copy));
									}

									program.Add(new Assembly.LDR(Assembly.Register.LR, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(f.ReturnType.Size))));
									program.Add(new Assembly.RET());

									return (program, data, types["void"]);
								}

							case "<-":
								{
									var (subProgramVal, subDataVal, valueTypeVal) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[2], memoryStart);

									int varLocPosition = memoryStart + SizeToWordBytes(valueTypeVal.Size);

									var (subProgramVar, subDataVar, valueTypeVar) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[1], varLocPosition);

									if (valueTypeVar is not PtrType) {
										throw new Exception("<- operator must receive a pointer type as its first operand");
									}

									if (!Type.AreEqual(((PtrType)valueTypeVar).ValueType, valueTypeVal)) {
										throw new Exception("<- operator operand types do not match");
									}

									program = program.Concat(subProgramVal).ToList();
									data = data.Concat(subDataVal).ToList();

									program = program.Concat(subProgramVar).ToList();
									data = data.Concat(subDataVar).ToList();

									program.Add(new Assembly.LDR(Assembly.Register.R1, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(varLocPosition))));

									if (valueTypeVal is IntType || valueTypeVal is PtrType) {
										program.Add(new Assembly.LDR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
										program.Add(new Assembly.STR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.R1)));
									} else {
										program.Add(new Assembly.ADD(Assembly.Register.R0, Assembly.Register.SP, new Assembly.ConstInt(memoryStart)));
										program.Add(new Assembly.MOV(Assembly.Register.R2, new Assembly.ConstInt(valueTypeVal.Size)));
										program.Add(new Assembly.BL(genericSubroutines.Copy));
									}

									return (program, data, valueTypeVal);
								}

							case "if":
								{
									Assembly.Label endLabel = new();

									for (int i = 1; i < listEl.Content.Count; i += 2) {
										Assembly.Label skip = new();

										if (i == listEl.Content.Count - 1) {
											i--;
										} else {
											var (subProgramCond, subDataCond, valueTypeCond) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[i], memoryStart);

											if (valueTypeCond is not IntType) {
												throw new Exception("condition expression must return an integer");
											}

											program = program.Concat(subProgramCond).ToList();
											data = data.Concat(subDataCond).ToList();

											program.Add(new Assembly.LDR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
											program.Add(new Assembly.CMP(Assembly.Register.R0, new Assembly.ConstInt(0)));
											program.Add(new Assembly.BEQ(skip));
										}

										var (subProgramExp, subDataExp, _) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[i + 1], memoryStart);

										program = program.Concat(subProgramExp).ToList();
										data = data.Concat(subDataExp).ToList();

										program.Add(new Assembly.B(endLabel));

										program.Add(skip);
									}

									program.Add(endLabel);

									return (program, data, types["void"]);
								}

							case "while":
								{
									Assembly.Label repeat = new();
									Assembly.Label skip = new();

									program.Add(repeat);

									var (subProgramCond, subDataCond, valueTypeCond) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[1], memoryStart);

									if (valueTypeCond is not IntType) {
										throw new Exception("condition expression must return an integer");
									}

									var (subProgramExp, subDataExp, _) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[2], memoryStart);

									program = program.Concat(subProgramCond).ToList();
									data = data.Concat(subDataCond).ToList();

									program.Add(new Assembly.LDR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
									program.Add(new Assembly.CMP(Assembly.Register.R0, new Assembly.ConstInt(0)));
									program.Add(new Assembly.BEQ(skip));

									program = program.Concat(subProgramExp).ToList();
									data = data.Concat(subDataExp).ToList();

									program.Add(new Assembly.B(repeat));
									program.Add(skip);

									return (program, data, types["void"]);
								}

							case "print":
								{
									var (subProgram, subData, valueType) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[1], memoryStart);

									program = program.Concat(subProgram).ToList();
									data = data.Concat(subData).ToList();

									switch (valueType) {
										case IntType intType:
											program.Add(new Assembly.LDR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
											program.Add(new Assembly.STR(Assembly.Register.R0, Assembly.SpecialConstant.WriteSignedNum));
											break;

										case CharType charType:
											program.Add(new Assembly.LDRB(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
											program.Add(new Assembly.STRB(Assembly.Register.R0, Assembly.SpecialConstant.WriteChar));
											break;

										case ArrayType arrType:
											{
												if (arrType.ItemType is not CharType charType) {
													throw new Exception("cannot print arrays apart from char arrays");
												}

												program.Add(new Assembly.ADD(Assembly.Register.R0, Assembly.Register.SP, new Assembly.ConstInt(memoryStart)));
												program.Add(new Assembly.STR(Assembly.Register.R0, Assembly.SpecialConstant.WriteString));
											}
											break;

										case PtrType ptrType:
											{
												if (ptrType.ValueType is not CharType charType) {
													throw new Exception("cannot print pointers apart from char pointers");
												}

												program.Add(new Assembly.LDR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
												program.Add(new Assembly.STR(Assembly.Register.R0, Assembly.SpecialConstant.WriteString));
											}
											break;

										default:
											throw new Exception("cannot print value of base type " + valueType.GetType());
									}

									return (program, data, valueType);
								}

							case "cast":
								{
									var (subProgram, subData, valueType) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[2], memoryStart);

									program = program.Concat(subProgram).ToList();
									data = data.Concat(subData).ToList();

									Type? newType = ConstructType(types, (ListElement)listEl.Content[1]);

									if (newType == null) {
										throw new Exception("could not resolve cast type");
									}

									return (program, data, (Type)newType);
								}

							case "@":
							case "@@":
								{
									var (subProgramPtr, subDataPtr, valueTypePtr) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[1], memoryStart);

									if (valueTypePtr is not PtrType ptrType) {
										throw new Exception("operators @ and @@ only works on a pointer and an int");
									}

									program = program.Concat(subProgramPtr).ToList();
									data = data.Concat(subDataPtr).ToList();

									var (subProgramInt, subDataInt, valueTypeInt) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[2], memoryStart + 4);

									if (valueTypeInt is not IntType) {
										throw new Exception("operators @ and @@ only works on a pointer and an int");
									}

									program = program.Concat(subProgramInt).ToList();
									data = data.Concat(subDataInt).ToList();

									Assembly.Label repeatLabel = new();
									Assembly.Label endLabel = new();

									program.Add(new Assembly.LDR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
									program.Add(new Assembly.LDR(Assembly.Register.R1, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart + 4))));
									program.Add(new Assembly.MOV(Assembly.Register.R2, new Assembly.ConstInt(ptrType.ValueType.Size)));
									program.Add(repeatLabel);
									program.Add(new Assembly.CMP(Assembly.Register.R2, new Assembly.ConstInt(0)));
									program.Add(new Assembly.BEQ(endLabel));
									program.Add(new Assembly.ADD(Assembly.Register.R0, Assembly.Register.R0, Assembly.Register.R1));
									program.Add(new Assembly.SUB(Assembly.Register.R2, Assembly.Register.R2, new Assembly.ConstInt(1)));
									program.Add(new Assembly.B(repeatLabel));
									program.Add(endLabel);
									program.Add(new Assembly.STR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));

									if (opName == "@@") {
										if (ptrType.ValueType is not ArrayType arrType) {
											throw new Exception("@@ only works on array pointers");
										}

										return (program, data, new PtrType(null, arrType.ItemType));
									} else {
										return (program, data, new PtrType(null, ptrType.ValueType));
									}
								}

							case "?":
								{
									var (subProgramCond, subDataCond, valueTypeCond) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[1], memoryStart);

									if (valueTypeCond is not IntType) {
										throw new Exception("only int types are allowed in a condition");
									}

									var (subProgramTrue, subDataTrue, valueTypeTrue) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[2], memoryStart);

									var (subProgramFalse, subDataFalse, valueTypeFalse) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[3], memoryStart);

									if (!Type.AreEqual(valueTypeTrue, valueTypeFalse)) {
										throw new Exception("conditional operator return types must match");
									}

									program = program.Concat(subProgramCond).ToList();
									data = data.Concat(subDataCond).ToList();

									Assembly.Label isFalse = new();

									program.Add(new Assembly.LDR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
									program.Add(new Assembly.CMP(Assembly.Register.R0, new Assembly.ConstInt(0)));
									program.Add(new Assembly.BEQ(isFalse));

									program = program.Concat(subProgramTrue).ToList();
									data = data.Concat(subDataTrue).ToList();

									program.Add(isFalse);

									program = program.Concat(subProgramFalse).ToList();
									data = data.Concat(subDataFalse).ToList();

									return (program, data, valueTypeTrue);
								}

							case ".":
								{
									var (subProgram, subData, valueType) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[1], memoryStart);

									if (valueType is not PtrType ptrType || ptrType.ValueType is not StructType structType) {
										throw new Exception("operator . only works on struct pointers");
									}

									program = program.Concat(subProgram).ToList();
									data = data.Concat(subData).ToList();

									Field? structField = null;

									int position = 0;

									for (int i = 2; i < listEl.Content.Count; i++) {
										structField = structType.Fields[((WordElement)listEl.Content[2]).Content];

										position += ((Field)structField).Position;

										if (i + 1 == listEl.Content.Count) break;

										if (((Field)structField).Type is not StructType) {
											throw new Exception("you need to learn how to use . correctly");
										}

										structType = (StructType)((Field)structField).Type;
									}

									if (structField == null) {
										throw new Exception("idiot");
									}

									program.Add(new Assembly.LDR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
									program.Add(new Assembly.ADD(Assembly.Register.R0, Assembly.Register.R0, new Assembly.ConstInt(position)));
									program.Add(new Assembly.STR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));

									return (program, data, ((Field)structField).Type);
								}

							case "$":
								{
									var (subProgram, subData, valueType) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[1], memoryStart);

									if (valueType is not PtrType ptrType) {
										throw new Exception("cannot dereference a non-pointer");
									}

									program = program.Concat(subProgram).ToList();
									data = data.Concat(subData).ToList();

									program.Add(new Assembly.LDR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
									program.Add(new Assembly.ADD(Assembly.Register.R1, Assembly.Register.SP, new Assembly.ConstInt(memoryStart)));
									program.Add(new Assembly.MOV(Assembly.Register.R2, new Assembly.ConstInt(ptrType.ValueType.Size)));
									program.Add(new Assembly.BL(genericSubroutines.Copy));

									return (program, data, ptrType.ValueType);
								}

							case "+":
								{
									Assembly.Label add = new();
									Assembly.Label allCompleted = new();

									for (int i = 1; i < listEl.Content.Count; i++) {
										var (subProgram, subData, valueType) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[i], i == 1 ? memoryStart : memoryStart + 4);

										if (valueType is not IntType) {
											throw new Exception("can only add integer types");
										}

										program = program.Concat(subProgram).ToList();
										data = data.Concat(subData).ToList();

										if (i != 1) {
											program.Add(new Assembly.BL(add));
										}
									}

									program.Add(new Assembly.B(allCompleted));

									program.Add(add);

									program.Add(new Assembly.LDR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
									program.Add(new Assembly.LDR(Assembly.Register.R1, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart + 4))));

									program.Add(new Assembly.ADD(Assembly.Register.R0, Assembly.Register.R0, Assembly.Register.R1));

									program.Add(new Assembly.STR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));

									program.Add(new Assembly.RET());

									program.Add(allCompleted);

									return (program, data, types["int"]);
								}

							case "-":
								{
									Assembly.Label sub = new();
									Assembly.Label allCompleted = new();

									for (int i = 1; i < listEl.Content.Count; i++) {
										var (subProgram, subData, valueType) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[i], i == 1 ? memoryStart : memoryStart + 4);

										if (valueType is not IntType) {
											throw new Exception("can only add integer types");
										}

										program = program.Concat(subProgram).ToList();
										data = data.Concat(subData).ToList();

										if (i != 1) {
											program.Add(new Assembly.BL(sub));
										}
									}

									program.Add(new Assembly.B(allCompleted));

									program.Add(sub);

									program.Add(new Assembly.LDR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
									program.Add(new Assembly.LDR(Assembly.Register.R1, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart + 4))));

									program.Add(new Assembly.SUB(Assembly.Register.R0, Assembly.Register.R0, Assembly.Register.R1));

									program.Add(new Assembly.STR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));

									program.Add(new Assembly.RET());

									program.Add(allCompleted);

									return (program, data, types["int"]);
								}

							case "*":
								{
									Assembly.Label multiply = new();
									Assembly.Label allCompleted = new();

									for (int i = 1; i < listEl.Content.Count; i++) {
										var (subProgram, subData, valueType) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[i], i == 1 ? memoryStart : memoryStart + 4);

										if (valueType is not IntType) {
											throw new Exception("can only multiply integer types");
										}

										program = program.Concat(subProgram).ToList();
										data = data.Concat(subData).ToList();

										if (i != 1 && listEl.Content.Count > 3) {
											program.Add(new Assembly.BL(multiply));
										}
									}

									if (listEl.Content.Count > 2) {
										if (listEl.Content.Count > 3) {
											program.Add(new Assembly.B(allCompleted));
										}

										program.Add(multiply);

										program.Add(new Assembly.MOV(Assembly.Register.R2, new Assembly.ConstInt(0)));
										program.Add(new Assembly.LDR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
										program.Add(new Assembly.LDR(Assembly.Register.R1, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart + 4))));

										Assembly.Label skipSignSwap = new();
										Assembly.Label done = new();

										program.Add(new Assembly.CMP(Assembly.Register.R1, new Assembly.ConstInt(0)));
										program.Add(new Assembly.BGT(skipSignSwap));
										program.Add(new Assembly.SUB(Assembly.Register.R0, Assembly.Register.R2, Assembly.Register.R0));
										program.Add(new Assembly.SUB(Assembly.Register.R1, Assembly.Register.R2, Assembly.Register.R1));
										program.Add(skipSignSwap);

										Assembly.Label repeat = new();

										program.Add(repeat);
										program.Add(new Assembly.CMP(Assembly.Register.R1, new Assembly.ConstInt(0)));
										program.Add(new Assembly.BEQ(done));
										program.Add(new Assembly.ADD(Assembly.Register.R2, Assembly.Register.R2, Assembly.Register.R0));
										program.Add(new Assembly.SUB(Assembly.Register.R1, Assembly.Register.R1, new Assembly.ConstInt(1)));
										program.Add(new Assembly.B(repeat));
										program.Add(done);

										program.Add(new Assembly.STR(Assembly.Register.R2, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));

										if (listEl.Content.Count > 3) {
											program.Add(new Assembly.RET());

											program.Add(allCompleted);
										}
									}

									return (program, data, types["int"]);
								}

							case "<":
							case ">":
							case "<=":
							case ">=":
								{
									var (subProgramA, subDataA, valueTypeA) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[1], memoryStart);

									var (subProgramB, subDataB, valueTypeB) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[2], memoryStart + 4);

									if (valueTypeA is not IntType || valueTypeB is not IntType) {
										throw new Exception("can only compare integer types");
									}

									program = program.Concat(subProgramA).ToList();
									data = data.Concat(subDataA).ToList();

									program = program.Concat(subProgramB).ToList();
									data = data.Concat(subDataB).ToList();

									Assembly.Label skipFalse = new();

									program.Add(new Assembly.LDR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
									program.Add(new Assembly.LDR(Assembly.Register.R1, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart + 4))));

									program.Add(new Assembly.CMP(Assembly.Register.R0, Assembly.Register.R1));

									program.Add(new Assembly.MOV(Assembly.Register.R2, new Assembly.ConstInt(1)));

									if (opName == "<" || opName == "<=") {
										program.Add(new Assembly.BLT(skipFalse));
									} else {
										program.Add(new Assembly.BGT(skipFalse));
									}

									if (opName == "<=" || opName == ">=") {
										program.Add(new Assembly.BEQ(skipFalse));
									}

									program.Add(new Assembly.MOV(Assembly.Register.R2, new Assembly.ConstInt(0)));
									program.Add(skipFalse);

									program.Add(new Assembly.STR(Assembly.Register.R2, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));

									return (program, data, types["int"]);
								}

							case "&&":
							case "||":
								{
									var (subProgramA, subDataA, valueTypeA) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[1], memoryStart);

									var (subProgramB, subDataB, valueTypeB) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[2], memoryStart);

									if (valueTypeA is not IntType || valueTypeB is not IntType) {
										throw new Exception("can only perform logical operations on integer types");
									}

									program = program.Concat(subProgramA).ToList();
									data = data.Concat(subDataA).ToList();

									Assembly.Label skipB = new();

									program.Add(new Assembly.LDR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
									program.Add(new Assembly.CMP(Assembly.Register.R0, new Assembly.ConstInt(0)));
									program.Add(opName == "&&" ? new Assembly.BEQ(skipB) : new Assembly.BNE(skipB));

									program = program.Concat(subProgramB).ToList();
									data = data.Concat(subDataB).ToList();

									program.Add(skipB);

									return (program, data, types["int"]);
								}

							case "size_of":
								{
									Type? type = ConstructType(types, (ListElement)listEl.Content[1]);

									if (type == null) {
										throw new Exception("failed to resolve type in sizeof");
									}

									program.Add(new Assembly.MOV(Assembly.Register.R0, new Assembly.ConstInt(type.Size)));
									program.Add(new Assembly.STR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));

									return (program, data, types["int"]);
								}

							case "size_of_value":
								{
									var (_, _, type) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[1], memoryStart);

									program.Add(new Assembly.MOV(Assembly.Register.R0, new Assembly.ConstInt(type.Size)));
									program.Add(new Assembly.STR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));

									return (program, data, types["int"]);
								}

							case "!":
								{
									var (subProgram, subData, valueType) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[1], memoryStart);

									if (valueType is not IntType) {
										throw new Exception("can only perform logical operations on integer types");
									}

									program = program.Concat(subProgram).ToList();
									data = data.Concat(subData).ToList();

									Assembly.Label skipTrue = new();

									program.Add(new Assembly.LDR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
									program.Add(new Assembly.MOV(Assembly.Register.R1, new Assembly.ConstInt(1)));
									program.Add(new Assembly.CMP(Assembly.Register.R0, new Assembly.ConstInt(0)));
									program.Add(new Assembly.BEQ(skipTrue));
									program.Add(new Assembly.MOV(Assembly.Register.R1, new Assembly.ConstInt(0)));
									program.Add(skipTrue);
									program.Add(new Assembly.STR(Assembly.Register.R1, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));

									return (program, data, types["int"]);
								}

							case "&":
							case "^":
							case "|":
								{
									var (subProgramA, subDataA, valueTypeA) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[1], memoryStart);

									var (subProgramB, subDataB, valueTypeB) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[2], memoryStart + 4);

									if (valueTypeA is not IntType || valueTypeB is not IntType) {
										throw new Exception("can only perform logical operations on integer types");
									}

									program = program.Concat(subProgramA).ToList();
									data = data.Concat(subDataA).ToList();

									program = program.Concat(subProgramB).ToList();
									data = data.Concat(subDataB).ToList();

									program.Add(new Assembly.LDR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
									program.Add(new Assembly.LDR(Assembly.Register.R1, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart + 4))));

									switch (opName) {
										case "&":
											program.Add(new Assembly.AND(Assembly.Register.R0, Assembly.Register.R0, Assembly.Register.R1));
											break;
										case "^":
											program.Add(new Assembly.XOR(Assembly.Register.R0, Assembly.Register.R0, Assembly.Register.R1));
											break;
										case "|":
											program.Add(new Assembly.OR(Assembly.Register.R0, Assembly.Register.R0, Assembly.Register.R1));
											break;
									}

									program.Add(new Assembly.STR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));

									return (program, data, types["int"]);
								}

							case "<<":
							case ">>":
							case ">>>":
								{
									var (subProgramA, subDataA, valueTypeA) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[1], memoryStart);

									var (subProgramB, subDataB, valueTypeB) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[2], memoryStart + 4);

									if (valueTypeA is not IntType || valueTypeB is not IntType) {
										throw new Exception("can only perform logical shift operations on integer types");
									}

									program = program.Concat(subProgramA).ToList();
									data = data.Concat(subDataA).ToList();

									program = program.Concat(subProgramB).ToList();
									data = data.Concat(subDataB).ToList();

									program.Add(new Assembly.LDR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
									program.Add(new Assembly.LDR(Assembly.Register.R1, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart + 4))));

									if (opName == "<<") {
										program.Add(new Assembly.LSL(Assembly.Register.R3, Assembly.Register.R0, Assembly.Register.R1));
									} else {
										program.Add(new Assembly.LSR(Assembly.Register.R3, Assembly.Register.R0, Assembly.Register.R1));

										if (opName == ">>") {
											program.Add(new Assembly.CMP(Assembly.Register.R3, new Assembly.ConstInt(0)));

											Assembly.Label skipBitProp = new();

											program.Add(new Assembly.BGT(skipBitProp));
											program.Add(new Assembly.BEQ(skipBitProp));
											program.Add(new Assembly.MOV(Assembly.Register.R2, new Assembly.ConstInt(-1)));
											program.Add(new Assembly.XOR(Assembly.Register.R3, Assembly.Register.R3, Assembly.Register.R2));
											program.Add(new Assembly.LSR(Assembly.Register.R2, Assembly.Register.R2, Assembly.Register.R1));
											program.Add(new Assembly.XOR(Assembly.Register.R3, Assembly.Register.R3, Assembly.Register.R2));
											program.Add(skipBitProp);
										}
									}

									program.Add(new Assembly.STR(Assembly.Register.R3, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));

									return (program, data, types["int"]);
								}

							case "!=":
							case "==":
								{
									int trueValue = opName == "==" ? 1 : 0;
									int falseValue = 1 - trueValue;

									var (subProgramA, subDataA, valueTypeA) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[1], memoryStart);

									var (subProgramB, subDataB, valueTypeB) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[2], memoryStart + SizeToWordBytes(valueTypeA.Size));

									if (!Type.AreEqual(valueTypeA, valueTypeB)) {
										throw new Exception("the types of two values must be equal if they are to be compared");
									}

									program = program.Concat(subProgramA).ToList();
									data = data.Concat(subDataA).ToList();

									program = program.Concat(subProgramB).ToList();
									data = data.Concat(subDataB).ToList();

									if (valueTypeA is VoidType) {
										throw new Exception("cannot compare void types");
									}

									if (valueTypeA.Size == 0) {
										program.Add(new Assembly.MOV(Assembly.Register.R0, new Assembly.ConstInt(trueValue)));
										program.Add(new Assembly.STR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));

										return (program, data, types["int"]);
									}

									if (valueTypeA.Size == 4) {
										program.Add(new Assembly.LDR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
										program.Add(new Assembly.LDR(Assembly.Register.R1, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart + 4))));
										program.Add(new Assembly.CMP(Assembly.Register.R0, Assembly.Register.R1));

										Assembly.Label skipFalse = new();

										program.Add(new Assembly.MOV(Assembly.Register.R2, new Assembly.ConstInt(trueValue)));
										program.Add(new Assembly.BEQ(skipFalse));
										program.Add(new Assembly.MOV(Assembly.Register.R2, new Assembly.ConstInt(falseValue)));
										program.Add(skipFalse);
										program.Add(new Assembly.STR(Assembly.Register.R2, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));

										return (program, data, types["int"]);
									}

									program.Add(new Assembly.ADD(Assembly.Register.R2, Assembly.Register.SP, new Assembly.ConstInt(memoryStart)));
									program.Add(new Assembly.MOV(Assembly.Register.R3, new Assembly.ConstInt(valueTypeA.Size)));

									program.Add(new Assembly.MOV(Assembly.Register.R4, new Assembly.ConstInt(trueValue)));

									Assembly.Label repeat = new();
									Assembly.Label isFalse = new();
									Assembly.Label exit = new();

									program.Add(repeat);
									program.Add(new Assembly.CMP(Assembly.Register.R3, new Assembly.ConstInt(0)));
									program.Add(new Assembly.BEQ(exit));
									program.Add(new Assembly.LDRB(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.R2)));
									program.Add(new Assembly.LDRB(Assembly.Register.R1, new Assembly.IndexedMemoryLocation(Assembly.Register.R2, new Assembly.ConstInt(SizeToWordBytes(valueTypeA.Size)))));
									program.Add(new Assembly.CMP(Assembly.Register.R0, Assembly.Register.R1));
									program.Add(new Assembly.BNE(isFalse));
									program.Add(new Assembly.SUB(Assembly.Register.R3, Assembly.Register.R3, new Assembly.ConstInt(1)));
									program.Add(new Assembly.B(repeat));

									program.Add(isFalse);

									program.Add(new Assembly.MOV(Assembly.Register.R4, new Assembly.ConstInt(falseValue)));

									program.Add(exit);

									program.Add(new Assembly.STR(Assembly.Register.R4, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));

									return (program, data, types["int"]);
								}

							case "/":
							case "%":
								{
									var (subProgramN, subDataN, valueTypeN) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[1], memoryStart);

									if (valueTypeN is not IntType) {
										throw new Exception("division only works on integers");
									}

									program = program.Concat(subProgramN).ToList();
									data = data.Concat(subDataN).ToList();

									var (subProgramD, subDataD, valueTypeD) = CompileExpression(genericSubroutines, types, functions, f, variables, new ListElement(new List<Element> {new WordElement("*")}.Concat(listEl.Content.Skip(2).ToList()).ToList()), memoryStart + 4);

									program = program.Concat(subProgramD).ToList();
									data = data.Concat(subDataD).ToList();

									program.Add(new Assembly.LDR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
									program.Add(new Assembly.LDR(Assembly.Register.R1, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart + 4))));

									Assembly.Label skipSignSwapN = new();

									program.Add(new Assembly.MOV(Assembly.Register.R2, new Assembly.ConstInt(0)));
									program.Add(new Assembly.MOV(Assembly.Register.R3, new Assembly.ConstInt(0)));

									program.Add(new Assembly.CMP(Assembly.Register.R0, new Assembly.ConstInt(0)));
									program.Add(new Assembly.BGT(skipSignSwapN));
									program.Add(new Assembly.SUB(Assembly.Register.R0, Assembly.Register.R2, Assembly.Register.R0));
									program.Add(new Assembly.MOV(Assembly.Register.R3, new Assembly.ConstInt(1)));
									program.Add(skipSignSwapN);

									if (opName == "/") {
										Assembly.Label skipSignSwapD = new();

										program.Add(new Assembly.CMP(Assembly.Register.R1, new Assembly.ConstInt(0)));
										program.Add(new Assembly.BGT(skipSignSwapD));
										program.Add(new Assembly.SUB(Assembly.Register.R1, Assembly.Register.R2, Assembly.Register.R1));
										program.Add(new Assembly.SUB(Assembly.Register.R3, Assembly.Register.R3, new Assembly.ConstInt(1)));
										program.Add(skipSignSwapD);
									}

									Assembly.Label repeat = new();
									Assembly.Label end = new();

									program.Add(repeat);

									program.Add(new Assembly.CMP(Assembly.Register.R0, Assembly.Register.R1));
									program.Add(new Assembly.BLT(end));

									program.Add(new Assembly.SUB(Assembly.Register.R0, Assembly.Register.R0, Assembly.Register.R1));

									if (opName == "/") {
										program.Add(new Assembly.ADD(Assembly.Register.R2, Assembly.Register.R2, new Assembly.ConstInt(1)));
									}

									program.Add(new Assembly.B(repeat));

									program.Add(end);

									Assembly.Label skipResultSignFlip = new();

									if (opName == "/") {
										program.Add(new Assembly.CMP(Assembly.Register.R3, new Assembly.ConstInt(0)));
										program.Add(new Assembly.BEQ(skipResultSignFlip));
										program.Add(new Assembly.MOV(Assembly.Register.R3, new Assembly.ConstInt(0)));
										program.Add(new Assembly.SUB(Assembly.Register.R2, Assembly.Register.R3, Assembly.Register.R2));
										program.Add(skipResultSignFlip);
										program.Add(new Assembly.STR(Assembly.Register.R2, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
									} else {
										program.Add(new Assembly.CMP(Assembly.Register.R3, new Assembly.ConstInt(0)));
										program.Add(new Assembly.BEQ(skipResultSignFlip));
										program.Add(new Assembly.MOV(Assembly.Register.R3, new Assembly.ConstInt(0)));
										program.Add(new Assembly.SUB(Assembly.Register.R0, Assembly.Register.R3, Assembly.Register.R0));
										program.Add(skipResultSignFlip);
										program.Add(new Assembly.STR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
									}

									return (program, data, types["int"]);
								}

							default:
								{
									if (functions.ContainsKey(opName)) {
										Function g = functions[opName];

										if (g.Parameters.Count + 1 != listEl.Content.Count) {
											throw new Exception("argument count mismatch");
										}

										{
											int i = 1;

											int position = memoryStart + g.ReturnType.Size + 4;

											foreach (Field parameter in g.Parameters.Values) {
												var (subProgram, subData, paramType) = CompileExpression(genericSubroutines, types, functions, f, variables, listEl.Content[i], position);

												if (!Type.AreEqual(paramType, parameter.Type)) {
													throw new Exception("parameter type mismatch");
												}

												program = program.Concat(subProgram).ToList();
												data = data.Concat(subData).ToList();

												position += SizeToWordBytes(parameter.Type.Size);
												i++;
											}
										}

										program.Add(new Assembly.ADD(Assembly.Register.SP, Assembly.Register.SP, new Assembly.ConstInt(memoryStart)));
										program.Add(new Assembly.BL(g.Label));
										program.Add(new Assembly.SUB(Assembly.Register.SP, Assembly.Register.SP, new Assembly.ConstInt(memoryStart)));

										return (program, data, g.ReturnType);
									}
									break;
								}
						}

						throw new Exception("no operator or function found with name " + opName);
					}

				case IntElement intEl:
					program.Add(new Assembly.MOV(Assembly.Register.R0, new Assembly.ConstInt(intEl.Content)));
					program.Add(new Assembly.STR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));
					return (program, data, types["int"]);

				case WordElement wordEl:
					{
						if (wordEl.Content.StartsWith(".")) {
							Assembly.SpecialConstant c;
							Type t;

							switch (wordEl.Content) {
								default:
									c = new Assembly.SpecialConstant(wordEl.Content);
									t = types["int"];
									break;
							}

							program.Add(new Assembly.MOV(Assembly.Register.R0, c));
							program.Add(new Assembly.STR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));

							return (program, data, new PtrType(null, t));
						}

						Field field = variables[wordEl.Content.StartsWith("$") ? wordEl.Content.Substring(1) : wordEl.Content];

						program.Add(new Assembly.ADD(Assembly.Register.R0, Assembly.Register.SP, new Assembly.ConstInt(field.Position)));

						if (wordEl.Content.StartsWith("$")) {
							program.Add(new Assembly.ADD(Assembly.Register.R1, Assembly.Register.SP, new Assembly.ConstInt(memoryStart)));
							program.Add(new Assembly.MOV(Assembly.Register.R2, new Assembly.ConstInt(field.Type.Size)));
							program.Add(new Assembly.BL(genericSubroutines.Copy));

							return (program, data, field.Type);
						} else {
							program.Add(new Assembly.STR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));

							return (program, data, new PtrType(null, field.Type));
						}
					}

				case StringElement stringEl:
					{
						Assembly.Label label = new();

						data.Add(label);
						data.Add(new Assembly.String(stringEl.Content));

						program.Add(new Assembly.MOV(Assembly.Register.R0, label));
						program.Add(new Assembly.STR(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));

						return (program, data, new PtrType(null, types["char"]));
					}

				case CharElement charEl:
					{
						program.Add(new Assembly.MOV(Assembly.Register.R0, new Assembly.ConstInt((int)charEl.Content)));
						program.Add(new Assembly.STRB(Assembly.Register.R0, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(memoryStart))));

						return (program, data, types["char"]);
					}
			}

			throw new Exception("cannot handle expression element type " + expression.GetType());
		}

		public static string Compile(ListElement root) {
			Dictionary<string, Type> types = new Dictionary<string, Type> {
				{"void", new VoidType("void")},
				{"int", new IntType("int")},
				{"float", new FloatType("float")},
				{"char", new CharType("char")},
			};

			Dictionary<string, Function> functions = new Dictionary<string, Function>();

			{
				int oldFailures = 0;

				int failures;

				do {
					failures = 0;

					foreach (ListElement dec in ((ListElement)root.Content[0]).Content) {
						Type? newType = ConstructType(types, (ListElement)dec.Content[1], false, false);

						if (newType == null) {
							failures++;
							continue;
						}

						newType.Name = ((WordElement)dec.Content[0]).Content;

						if (types.ContainsKey(newType.Name)) {
							throw new Exception("cannot overwrite type with name " + newType.Name);
						}

						types[newType.Name] = newType;
					}

					if (failures > 0 && oldFailures == failures) {
						throw new Exception("too many type construction failures");
					}

					oldFailures = failures;
				} while (failures > 0);
			}

			foreach (Type type in types.Values) {
				ResolvePtrTypes(types, type);
			}

			foreach (ListElement dec in ((ListElement)root.Content[1]).Content) {
				string name = ((WordElement)dec.Content[1]).Content;

				Type? returnType = ConstructType(types, (ListElement)dec.Content[0]);

				if (returnType == null) {
					throw new Exception("could not construct function return type");
				}

				List<Field> parameters = new List<Field>();

				int position = returnType.Size + 4;

				foreach (ListElement parameterData in ((ListElement)dec.Content[2]).Content) {
					Field? field = ConstructField(types, parameterData, position, true);

					if (field == null) {
						throw new Exception("failed to construct parameter");
					}

					parameters.Add((Field)field);

					position += SizeToWordBytes(((Field)field).Type.Size);
				}

				functions[name] = new Function(name, returnType, parameters, dec.Content[3]);
			}

			List<Assembly.Line> program = new();
			List<Assembly.Line> data = new();

			Assembly.Label stackLabel = new Assembly.Label();

			program.Add(new Assembly.MOV(Assembly.Register.SP, stackLabel));

			if (!functions.ContainsKey("main")) {
				throw new Exception("no main function was declared");
			}

			program.Add(new Assembly.BL(functions["main"].Label));

			program.Add(new Assembly.HALT());

			GenericSubroutines genericSubroutines;

			genericSubroutines.Copy = new Assembly.Label();

			{
				Assembly.Label end = new Assembly.Label();

				program.Add(genericSubroutines.Copy);
				program.Add(new Assembly.CMP(Assembly.Register.R2, new Assembly.ConstInt(0)));
				program.Add(new Assembly.BEQ(end));
				program.Add(new Assembly.LDRB(Assembly.Register.R3, new Assembly.IndexedMemoryLocation(Assembly.Register.R0)));
				program.Add(new Assembly.STRB(Assembly.Register.R3, new Assembly.IndexedMemoryLocation(Assembly.Register.R1)));
				program.Add(new Assembly.ADD(Assembly.Register.R0, Assembly.Register.R0, new Assembly.ConstInt(1)));
				program.Add(new Assembly.ADD(Assembly.Register.R1, Assembly.Register.R1, new Assembly.ConstInt(1)));
				program.Add(new Assembly.SUB(Assembly.Register.R2, Assembly.Register.R2, new Assembly.ConstInt(1)));
				program.Add(new Assembly.B(genericSubroutines.Copy));
				program.Add(end);
				program.Add(new Assembly.RET());
			}

			foreach (Function f in functions.Values) {
				program.Add(f.Label);

				program.Add(new Assembly.STR(Assembly.Register.LR, new Assembly.IndexedMemoryLocation(Assembly.Register.SP, new Assembly.ConstInt(f.ReturnType.Size))));

				var (subProgram, subData, _) = CompileExpression(genericSubroutines, types, functions, f, f.Parameters, f.Body, f.TotalParameterSize + f.ReturnType.Size + 4);

				program = program.Concat(subProgram).ToList();
				data = data.Concat(subData).ToList();
			}

			program = program.Concat(data).ToList();

			program.Add(new Assembly.ALIGN(4));

			program.Add(stackLabel);

			{
				Assembly.Label? oldLabel = null;

				for (int i = 0; i < program.Count; i++) {
					Assembly.Line line = program[i];

					if (line is Assembly.Label label) {
						if (oldLabel == null) {
							oldLabel = label;
						} else {
							label.ID = oldLabel.ID;
							program.RemoveAt(i);
							i--;
						}
					} else {
						oldLabel = null;
					}
				}
			}

			return string.Join("\n", program.Select(x => x.ToAssembly()));
		}

		private static bool IsNumberCharacter(char c) {
			return c >= 48 && c <= 57;
		}

		private static int HexCharValue(char c) {
			if (IsNumberCharacter(c)) {
				return c - 48;
			}

			if (c >= 65 && c <= 72) {
				return c - 55;
			}

			if (c >= 97 && c <= 104) {
				return c - 87;
			}

			throw new Exception("unrecognised hex digit");
		}

		private static int HexToInt(string hex) {
			int result = 0;

			int powerLevel = 1;

			for (int i = hex.Length - 1; i >= 0; i--) {
				char c = hex[i];

				if (c == '_') continue;

				if (c == 'x') return result;

				result += HexCharValue(c) * powerLevel;

				powerLevel *= 16;
			}

			throw new Exception("failed to parse hex string");
		}

		private static int BinToInt(string bin) {
			int result = 0;

			int powerLevel = 1;

			for (int i = bin.Length - 1; i >= 0; i--) {
				char c = bin[i];

				if (c == '_') continue;

				if (c == 'b') return result;

				result += Convert.ToInt32(c) * powerLevel;

				powerLevel *= 2;
			}

			throw new Exception("failed to parse hex string");
		}

		public static (ListElement result, int index) Parse(List<Token> tokens, int i = 0) {
			ListElement root = new ListElement(new List<Element>());

			for (; i < tokens.Count; i++) {
				Token token = tokens[i];

				switch (token.Type) {
					case TokenType.WORD:
						root.Content.Add(new WordElement(token.Content));
						break;

					case TokenType.INT:
						root.Content.Add(new IntElement(Int32.Parse(token.Content)));
						break;

					case TokenType.FLOAT:
						root.Content.Add(new FloatElement(Convert.ToSingle(token.Content)));
						break;

					case TokenType.HEX:
						root.Content.Add(new IntElement(HexToInt(token.Content)));
						break;

					case TokenType.BIN:
						root.Content.Add(new IntElement(BinToInt(token.Content)));
						break;

					case TokenType.STRING:
						root.Content.Add(new StringElement(token.Content));
						break;

					case TokenType.CHAR:
						root.Content.Add(new CharElement(token.Content[0]));
						break;

					case TokenType.BRACKET:
						if (token.Content[0] == ']') {
							return (root, i);
						} else {
							(Element result, int index) = Parse(tokens, i + 1);
							i = index;
							root.Content.Add(result);
						}
						break;

					default:
						throw new Exception("unrecognised token type: " + token.Type);
				}
			}

			return (root, i);
		}

		public static List<Token> Lex(string script) {
			script += "\n";

			List<Token> tokens = new List<Token>();

			Token currentToken = new Token(TokenType.WHITESPACE, "");

			bool escaped = false;
			bool inString = false;

			int commentLevel = 0;

			for (int i = 0; i < script.Length; i++) {
				char c = script[i];

				if (!inString) {
					switch (c) {
						case '{':
							commentLevel++;
							break;
						case '}':
							commentLevel--;
							break;
					}

					if (commentLevel < 0) {
						throw new Exception("extra end comment bracket");
					}
				}

				if ((((c == '"' || c == '\'') && !escaped) || !inString) && (c == '\n' || c == '\r' || c == ' ' || c == '\t' || c == '"' || c == '\'' || c == '[' || c == ']')) {
					if (inString || currentToken.Content.Length > 0) {
						if (inString && c == '\'' && currentToken.Content.Length != 1) {
							throw new Exception("only one character is permitted in character literal");
						}

						tokens.Add(currentToken);
						currentToken = new Token(TokenType.WHITESPACE, "");
					}

					currentToken.Type = TokenType.WHITESPACE;

					switch (c) {
						case '"':
						case '\'':
							if (inString) {
								inString = false;
							} else {
								inString = true;

								currentToken.Type = c == '"' ? TokenType.STRING : TokenType.CHAR;
							}
							break;
						case '[':
						case ']':
							tokens.Add(new Token(TokenType.BRACKET, c.ToString()));
							break;
					}
				} else {
					if (inString) {
						if (escaped) {
							switch (c) {
								case 'n':
									currentToken.Content += '\n';
									break;
								case 'r':
									currentToken.Content += '\r';
									break;
								case 't':
									currentToken.Content += '\t';
									break;
								case 'x':
									currentToken.Content += (char)(16 * HexCharValue(script[i + 1]) + HexCharValue(script[i + 2]));
									i += 2;
									break;

								default:
									currentToken.Content += c;
									break;
							}

							escaped = false;
						} else {
							if (c == '\\') {
								escaped = true;
							} else {
								currentToken.Content += c;
							}
						}
					} else {
						switch (c) {
							case '-':
								if (i + 1 < script.Length && IsNumberCharacter(script[i + 1])) {
									currentToken.Type = TokenType.INT;
								} else {
									currentToken.Type = TokenType.WORD;
								}
								break;

							case '.':
								if (currentToken.Type == TokenType.INT) {
									currentToken.Type = TokenType.FLOAT;
								} else {
									currentToken.Type = TokenType.WORD;
								}
								break;

							case '_':
								if (currentToken.Type == TokenType.WHITESPACE) {
									currentToken.Type = TokenType.WORD;
								}
								break;

							case 'x':
								if (currentToken.Type == TokenType.INT) {
									currentToken.Type = TokenType.HEX;
								} else {
									currentToken.Type = TokenType.WORD;
								}
								break;

							case 'b':
								if (currentToken.Type == TokenType.INT) {
									currentToken.Type = TokenType.BIN;
								} else {
									currentToken.Type = TokenType.WORD;
								}
								break;

							default:
								if (IsNumberCharacter(c)) {
									if (currentToken.Type == TokenType.WHITESPACE || currentToken.Type == TokenType.INT) {
										currentToken.Type = TokenType.INT;
									} else if (currentToken.Type == TokenType.FLOAT || currentToken.Type == TokenType.HEX || currentToken.Type == TokenType.BIN) {
									} else {
										currentToken.Type = TokenType.WORD;
									}
								} else if (currentToken.Type != TokenType.HEX) {
									currentToken.Type = TokenType.WORD;
								}
								break;
						}

						currentToken.Content += c;
					}
				}
			}

			return tokens;
		}
	}

	public static class Program {
		public static void Main(string[] args) {
			if (args.Length != 1) {
				throw new Exception("you must specify a code file to compile");
			}

			string script = new System.IO.StreamReader(args[0]).ReadToEnd();

			List<Token> tokens = Compilation.Lex(script);

			(ListElement parsed, _) = Compilation.Parse(tokens);

			string compiled = Compilation.Compile(parsed);

			Console.WriteLine(compiled);
		}
	}
}
