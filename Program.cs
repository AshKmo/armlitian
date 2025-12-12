namespace Armlitian;

public enum TokenType {
	WHITESPACE,
	BRACKET,
	WORD,
	STRING,
	CHAR,
	INT,
	FLOAT,
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
		return StringRep.Escape(Content);
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

	private static Field? ConstructField(Dictionary<string, Type> types, ListElement fieldData, int position, bool resolveImmediately) {
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
						position += ((Field)field).Type.Size;
					}

					return new StructType(null, fields);
				}
		}

		return null;
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

		foreach (Type type in types.Values) {
			Console.WriteLine(type);
		}

		Console.WriteLine();

		foreach (ListElement dec in ((ListElement)root.Content[1]).Content) {
			string name = ((WordElement)dec.Content[1]).Content;

			Type? returnType = ConstructType(types, (ListElement)dec.Content[0]);

			if (returnType == null) {
				throw new Exception("could not construct function return type");
			}

			List<Field> parameters = new List<Field>();

			int position = 0;

			foreach (ListElement parameterData in ((ListElement)dec.Content[2]).Content) {
				Field? field = ConstructField(types, parameterData, position, true);

				if (field == null) {
					throw new Exception("failed to construct parameter");
				}

				parameters.Add((Field)field);
				position += ((Field)field).Type.Size;
			}

			functions[name] = new Function(name, returnType, parameters, dec.Content[3]);

			Console.WriteLine(functions[name]);
		}

		return "";
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

						default:
							if (IsNumberCharacter(c)) {
								if (currentToken.Type == TokenType.WHITESPACE || currentToken.Type == TokenType.INT) {
									currentToken.Type = TokenType.INT;
								} else if (currentToken.Type == TokenType.FLOAT) {
								} else {
									currentToken.Type = TokenType.WORD;
								}
							} else {
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
		List<Token> tokens = Compilation.Lex(@"
				[
					[Asdf [struct [
						[[int] x]
						[[float] y]
					]]]

					[Bruh [void]]
				]

				[
					[[Asdf] bruh [ [[int] x] ] [
						[return $x]
					]]
				]
				");

		(ListElement parsed, _) = Compilation.Parse(tokens);

		Compilation.Compile(parsed);
	}
}
