module App

open Elmish
open Elmish.React
open Fable.Core.JsInterop

importSideEffects "@fontsource-variable/public-sans"
importSideEffects "@fontsource-variable/jetbrains-mono"
importSideEffects "./index.css"

Program.mkProgram Model.Init Model.Update View.Render
|> Program.withReactSynchronous "fable-root"
|> Program.run
