open System
open System.Windows.Forms
open System.Drawing

let WIDTH = 600
let HEIGHT = 400
let SCALE = 10

type Direction = Up | Down | Left | Right

[<Struct>]
type P(x: int, y: int) =
    member this.X = x
    member this.Y = y
    static member (+) ((l: P), (r: P)) = P(l.X+r.X, l.Y+r.Y)

type Snake = {Tail: P array; Score: int; Dir: Direction} with
    member this.Head = this.Tail.[0]
    member this.Rest = if this.Tail.Length >= 1 then this.Tail.[1..] else [| |]

type Apple = {Pos: P; Color: Color; Fun: Snake -> Snake}
let redApple = {Pos = P(15,15); Color = Color.Red
                Fun = (fun s -> {s with Score = s.Score+1})}
let greenApple = {Pos = P(15,15); Color = Color.Green
                  Fun = (fun s -> {s with Score = s.Score+3})}

let rng = new Random()

let dirToP = function
    | Up    ->  P(0, -1)
    | Down  ->  P(0, 1)
    | Left  ->  P(-1, 0)
    | Right ->  P(1, 0)

let keyToDir = function
    | 38 -> Some(Up)
    | 40 -> Some(Down)
    | 37 -> Some(Left)
    | 39 -> Some(Right)
    | _  -> None

let wrapScreen (n: P) =
    let wrap min max x =
        match x with
        | x when x > max -> min
        | x when x < min -> max
        | n -> n
    let x = wrap 0 (WIDTH/SCALE - 1) n.X 
    let y = wrap 0 (HEIGHT/SCALE - 1) n.Y
    P(x,y)

let move nw (snake: Snake) =
    let newHead = snake.Head + (dirToP nw) |> wrapScreen
    let mutable newTail = snake.Tail
    if nw = snake.Dir
    then newTail.[0] <- newHead
    else newTail <- Array.append [| newHead |] snake.Tail
    { snake with Tail = newTail; Dir = nw}

let randomApple() =
    let pos = P(rng.Next(0,WIDTH/SCALE), rng.Next(0, HEIGHT/SCALE))
    if rng.Next(0, 10) >= 8
    then {greenApple with Pos = pos} else {redApple with Pos = pos}

let eat apple (snake: Snake) =
    if snake.Head = apple.Pos
    then (randomApple(), apple.Fun snake) else (apple, snake)

let hit (snake: Snake) =
    Option.isSome <| Array.tryFind (fun e -> e = snake.Head) snake.Rest

type myForm() =
    inherit Form()
    do base.DoubleBuffered <- true

let form = new myForm(Text = "Snake", ClientSize = Size(WIDTH, HEIGHT))
let gr = form.CreateGraphics()

let mutable newDir = Right
let setNewDir (e: KeyEventArgs) =
    let dir = keyToDir e.KeyValue
    if Option.isSome dir then newDir <- Option.get dir

form.KeyDown |> Observable.add setNewDir

let draw apple (snake: Snake) =
    use blue = new SolidBrush(Color.Blue)
    use black = new SolidBrush(Color.Black)
    use acolor = new SolidBrush(apple.Color)
    use opac = new SolidBrush(Color.FromArgb(25, 255, 255, 255))
    use font = new Font("Arial", 16.0f)

    gr.FillRectangle(opac, 0, 0, WIDTH, HEIGHT)
    let mines = Array.map (fun (e: P) -> new Rectangle(e.X*SCALE, e.Y*SCALE, SCALE, SCALE)) snake.Tail
    gr.FillRectangles(black, mines)
    gr.FillRectangle(acolor, apple.Pos.X*SCALE, apple.Pos.Y*SCALE, SCALE, SCALE)
    gr.FillRectangle(blue, snake.Head.X*SCALE, snake.Head.Y*SCALE, SCALE, SCALE)
    gr.DrawString(snake.Score.ToString(), font, black, PointF(0.0f,0.0f))

let rec gameLoop((snake: Snake), apple) = async {
    let now = DateTime.Now
    let next = snake |> move newDir
    let apple, next = eat apple next
    draw apple next
    let dt = 64.0 - DateTime.Now.Subtract(now).TotalMilliseconds
    do! Async.Sleep(Math.Max(dt, 0.0) |> int)

    if hit snake
    then MessageBox.Show("Final score: " + snake.Score.ToString()) |> ignore
         Application.Restart()
    else return! gameLoop(next, apple) }

let snake = { Tail = [| P(0,0) |]; Score = 0; Dir = newDir }

[<STAThread>]
do Async.Start(gameLoop(snake, redApple))
   Application.Run(form)