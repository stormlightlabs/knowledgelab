module GraphView

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Domain
open Model

/// D3 force simulation interop bindings
module D3 =
  /// Import D3 module
  [<Import("*", from = "d3")>]
  let d3 : obj = jsNative

  /// Select an element
  let select (selector : string) : obj = d3?select selector

  /// Select all elements
  let selectAll (selector : string) : obj = d3?selectAll selector

  /// Create a force simulation
  let forceSimulation (nodes : GraphNode array) : obj = d3?forceSimulation nodes

  /// Force: many-body (charge) force
  let forceManyBody () : obj = d3?forceManyBody ()

  /// Force: link force
  let forceLink (links : GraphLink array) : obj =
    let force = d3?forceLink (links)
    force?id (fun (d : GraphNode) -> d.Id) |> ignore
    force

  /// Force: centering force
  let forceCenter (x : float, y : float) : obj = d3?forceCenter (x, y)

  /// Force: collision force
  let forceCollide (radius : float) : obj = d3?forceCollide (radius)

  /// Create zoom behavior
  let zoom () : obj = d3?zoom ()

/// Convert backend Graph to frontend GraphData
let graphToGraphData (graph : Graph) : GraphData =
  // Count connections for each node to determine degree
  let nodeDegrees =
    graph.Edges
    |> List.fold
      (fun acc edge ->
        acc
        |> Map.change edge.Source (fun count -> Some((count |> Option.defaultValue 0) + 1))
        |> Map.change edge.Target (fun count -> Some((count |> Option.defaultValue 0) + 1)))
      Map.empty

  let nodes =
    graph.Nodes
    |> List.mapi (fun i id -> {
      Id = id
      Label = id
      Group = 0
      Degree = nodeDegrees |> Map.tryFind id |> Option.defaultValue 0
      X = None
      Y = None
      Vx = None
      Vy = None
    })

  let links =
    graph.Edges
    |> List.map (fun edge -> { Source = edge.Source; Target = edge.Target; Value = 1.0 })

  { Nodes = nodes; Links = links }

/// SVG graph view component
let svgGraph (state : State) (dispatch : Msg -> unit) =
  let graphData =
    match state.Graph with
    | Some graph -> graphToGraphData graph
    | None -> { Nodes = []; Links = [] }

  let width = 800.0
  let height = 600.0

  React.useEffectOnce (fun () ->
    if not graphData.Nodes.IsEmpty then
      let nodes = Array.ofList graphData.Nodes
      let links = Array.ofList graphData.Links

      let simulation =
        D3.forceSimulation (nodes)
        |> fun sim ->
          sim?force ("charge", D3.forceManyBody () |> (fun f -> f?strength (-300.0)))
          |> ignore

          sim?force ("link", D3.forceLink (links) |> (fun f -> f?distance (100.0)))
          |> ignore

          sim?force ("center", D3.forceCenter (width / 2.0, height / 2.0)) |> ignore

          sim?force ("collide", D3.forceCollide (20.0)) |> ignore

          sim

      simulation?on (
        "tick",
        fun () ->
          D3.select "#graph-svg"
          |> fun svg ->
            svg?selectAll ".link"
            |> fun links ->
              links?attr ("x1", fun (d : obj) -> d?source?x)
              |> fun l -> l?attr ("y1", fun (d : obj) -> d?source?y)
              |> fun l -> l?attr ("x2", fun (d : obj) -> d?target?x)
              |> fun l -> l?attr ("y2", fun (d : obj) -> d?target?y)
              |> ignore

          D3.select "#graph-svg"
          |> fun svg ->
            svg?selectAll ".node"
            |> fun nodes ->
              nodes?attr ("cx", fun (d : GraphNode) -> d.X |> Option.defaultValue 0.0)
              |> fun n -> n?attr ("cy", fun (d : GraphNode) -> d.Y |> Option.defaultValue 0.0)
              |> ignore

          D3.select "#graph-svg"
          |> fun svg ->
            svg?selectAll ".node-label"
            |> fun labels ->
              labels?attr ("x", fun (d : GraphNode) -> d.X |> Option.defaultValue 0.0)
              |> fun l -> l?attr ("y", fun (d : GraphNode) -> d.Y |> Option.defaultValue 0.0)
              |> ignore
      )
      |> ignore)



  Html.div [
    prop.className "flex-1 flex flex-col bg-base00"
    prop.children [
      Html.div [
        prop.className "p-4 border-b border-base02"
        prop.children [
          Html.h1 [ prop.className "text-2xl font-bold text-base05"; prop.text "Graph View" ]
          Html.div [
            prop.className "text-sm text-base03 mt-1"
            prop.text $"{graphData.Nodes.Length} notes, {graphData.Links.Length} links"
          ]
        ]
      ]
      Html.div [
        prop.className "flex-1 flex items-center justify-center overflow-hidden"
        prop.children [
          if graphData.Nodes.IsEmpty then
            Html.div [
              prop.className "text-center text-base03"
              prop.children [
                Html.p [ prop.text "No graph data available" ]
                Html.button [
                  prop.className
                    "mt-4 px-4 py-2 bg-blue hover:bg-blue-bright text-base00 rounded transition-all"
                  prop.text "Load Graph"
                  prop.onClick (fun _ -> dispatch LoadGraph)
                ]
              ]
            ]
          else
            Interop.reactApi.createElement (
              "svg",
              createObj [
                "id" ==> "graph-svg"
                "width" ==> width
                "height" ==> height
                "className" ==> "border border-base02"
                "children"
                ==> [|
                  for link in graphData.Links do
                    Interop.reactApi.createElement (
                      "line",
                      createObj [
                        "className" ==> "link"
                        "stroke" ==> "#6b7089"
                        "strokeWidth" ==> 1.5
                        "data-source" ==> link.Source
                        "data-target" ==> link.Target
                      ]
                    )

                  for node in graphData.Nodes do
                    Interop.reactApi.createElement (
                      "circle",
                      createObj [
                        "className" ==> "node"
                        "r" ==> (5.0 + float node.Degree)
                        "fill" ==> "#84a0c6"
                        "stroke" ==> "#c6c8d1"
                        "strokeWidth" ==> 1.5
                        "data-id" ==> node.Id
                        "style" ==> createObj [ "cursor" ==> "pointer" ]
                        "onClick" ==> fun _ -> dispatch (SelectNote node.Id)
                      ]
                    )

                  for node in graphData.Nodes do
                    Interop.reactApi.createElement (
                      "text",
                      createObj [
                        "className" ==> "node-label"
                        "fill" ==> "#c6c8d1"
                        "fontSize" ==> "10px"
                        "textAnchor" ==> "middle"
                        "dy" ==> "-10"
                        "style" ==> createObj [ "pointerEvents" ==> "none" ]
                        "children" ==> node.Label
                      ]
                    )
                |]
              ]
            )
        ]
      ]
    ]
  ]

/// Render the graph view based on the current engine setting
let render (state : State) (dispatch : Msg -> unit) = svgGraph state dispatch
