module Icons

open Feliz

module SVG =
  [<Literal>]
  let AddTask =
    """<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 256 256">
<path fill="currentColor" d="m213.66 82.34l-56-56A8 8 0 0 0 152 24H56a16 16 0 0 0-16 16v176a16 16 0 0 0 16 16h144a16 16 0 0 0 16-16V88a8 8 0 0 0-2.34-5.66M160 51.31L188.69 80H160ZM200 216H56V40h88v48a8 8 0 0 0 8 8h48zm-32-80a8 8 0 0 1-8 8H96a8 8 0 0 1 0-16h64a8 8 0 0 1 8 8m0 32a8 8 0 0 1-8 8H96a8 8 0 0 1 0-16h64a8 8 0 0 1 8 8" />
</svg>"""

  [<Literal>]
  let Task =
    """<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 32 32">
<path fill="currentColor" d="m14 20.18l-3.59-3.59L9 18l5 5l9-9l-1.41-1.42z" />
<path fill="currentColor" d="M25 5h-3V4a2 2 0 0 0-2-2h-8a2 2 0 0 0-2 2v1H7a2 2 0 0 0-2 2v21a2 2 0 0 0 2 2h18a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2M12 4h8v4h-8Zm13 24H7V7h3v3h12V7h3Z" />
</svg>"""

  [<Literal>]
  let CompleteTask =
    """<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 32 32">
<path fill="currentColor" d="m22 27.18l-2.59-2.59L18 26l4 4l8-8l-1.41-1.41z" />
<path fill="currentColor" d="M25 5h-3V4a2.006 2.006 0 0 0-2-2h-8a2.006 2.006 0 0 0-2 2v1H7a2.006 2.006 0 0 0-2 2v21a2.006 2.006 0 0 0 2 2h9v-2H7V7h3v3h12V7h3v11h2V7a2.006 2.006 0 0 0-2-2m-5 3h-8V4h8Z" />
</svg>"""

  [<Literal>]
  let RemoveTask =
    """<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 32 32">
<path fill="currentColor" d="M26.41 25L30 21.41L28.59 20L25 23.59L21.41 20L20 21.41L23.59 25L20 28.59L21.41 30L25 26.41L28.59 30L30 28.59z" />
<path fill="currentColor" d="M25 5h-3V4a2.006 2.006 0 0 0-2-2h-8a2.006 2.006 0 0 0-2 2v1H7a2.006 2.006 0 0 0-2 2v21a2.006 2.006 0 0 0 2 2h9v-2H7V7h3v3h12V7h3v10h2V7a2.006 2.006 0 0 0-2-2m-5 3h-8V4h8Z" />
</svg>"""

  [<Literal>]
  let AddFile =
    """<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 256 256">
<path fill="currentColor" d="m213.66 82.34l-56-56A8 8 0 0 0 152 24H56a16 16 0 0 0-16 16v176a16 16 0 0 0 16 16h144a16 16 0 0 0 16-16V88a8 8 0 0 0-2.34-5.66M160 51.31L188.69 80H160ZM200 216H56V40h88v48a8 8 0 0 0 8 8h48zm-40-64a8 8 0 0 1-8 8h-16v16a8 8 0 0 1-16 0v-16h-16a8 8 0 0 1 0-16h16v-16a8 8 0 0 1 16 0v16h16a8 8 0 0 1 8 8" />
</svg>"""

  [<Literal>]
  let File =
    """<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 256 256">
<path fill="currentColor" d="m213.66 82.34l-56-56A8 8 0 0 0 152 24H56a16 16 0 0 0-16 16v176a16 16 0 0 0 16 16h144a16 16 0 0 0 16-16V88a8 8 0 0 0-2.34-5.66M160 51.31L188.69 80H160ZM200 216H56V40h88v48a8 8 0 0 0 8 8h48zm-32-80a8 8 0 0 1-8 8H96a8 8 0 0 1 0-16h64a8 8 0 0 1 8 8m0 32a8 8 0 0 1-8 8H96a8 8 0 0 1 0-16h64a8 8 0 0 1 8 8" />
</svg>"""

  [<Literal>]
  let RemoveFile =
    """<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 256 256">
<path fill="currentColor" d="m213.66 82.34l-56-56A8 8 0 0 0 152 24H56a16 16 0 0 0-16 16v176a16 16 0 0 0 16 16h144a16 16 0 0 0 16-16V88a8 8 0 0 0-2.34-5.66M160 51.31L188.69 80H160ZM200 216H56V40h88v48a8 8 0 0 0 8 8h48zm-42.34-82.34L139.31 152l18.35 18.34a8 8 0 0 1-11.32 11.32L128 163.31l-18.34 18.35a8 8 0 0 1-11.32-11.32L116.69 152l-18.35-18.34a8 8 0 0 1 11.32-11.32L128 140.69l18.34-18.35a8 8 0 0 1 11.32 11.32" />
</svg>"""

  [<Literal>]
  let CreateFolder =
    """<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 24 24">
<path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 19H5a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h4l3 3h7a2 2 0 0 1 2 2v3.5M16 19h6m-3-3v6" />
</svg>"""

  [<Literal>]
  let DeleteFolder =
    """<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 24 24">
<path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.5 19H5a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h4l3 3h7a2 2 0 0 1 2 2v4m1 9l-5-5m0 5l5-5" />
</svg>"""

  [<Literal>]
  let OpenFolder =
    """<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 24 24">
<path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m5 19l2.757-7.351A1 1 0 0 1 8.693 11H21a1 1 0 0 1 .986 1.164l-.996 5.211A2 2 0 0 1 19.026 19za2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h4l3 3h7a2 2 0 0 1 2 2v2" />
</svg>"""

  [<Literal>]
  let CloseFolder =
    """<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 24 24">
<path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 4h4l3 3h7a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2" />
</svg>"""

  [<Literal>]
  let Help =
    """<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 48 48">
  <g fill="none">
      <path stroke="currentColor" stroke-linejoin="round" stroke-width="4" d="M24 44a19.94 19.94 0 0 0 14.142-5.858A19.94 19.94 0 0 0 44 24a19.94 19.94 0 0 0-5.858-14.142A19.94 19.94 0 0 0 24 4A19.94 19.94 0 0 0 9.858 9.858A19.94 19.94 0 0 0 4 24a19.94 19.94 0 0 0 5.858 14.142A19.94 19.94 0 0 0 24 44Z" />
      <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="4" d="M24 28.625v-4a6 6 0 1 0-6-6" />
      <path fill="currentColor" fill-rule="evenodd" d="M24 37.625a2.5 2.5 0 1 0 0-5a2.5 2.5 0 0 0 0 5" clip-rule="evenodd" />
  </g>
</svg>"""

  [<Literal>]
  let Settings =
    """<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 24 24">
  <path fill="currentColor" fill-opacity="0.16" fill-rule="evenodd" d="M18.51 9.49h1.87c.88 0 1.6.72 1.6 1.6v1.8c0 .88-.72 1.6-1.6 1.6h-1.87q-.047.136-.106.269l-.034.081l1.32 1.32c.62.62.62 1.64 0 2.26l-1.27 1.27c-.62.62-1.64.62-2.26 0l-1.32-1.32q-.096.043-.2.08l-.15.06v1.87c0 .88-.72 1.6-1.6 1.6h-1.8c-.88 0-1.6-.72-1.6-1.6v-1.87q-.136-.047-.269-.106l-.081-.034l-1.32 1.32c-.62.62-1.64.62-2.26 0l-1.27-1.27c-.62-.62-.62-1.64 0-2.26l1.32-1.32q-.043-.096-.08-.2l-.06-.15H3.6c-.88 0-1.6-.72-1.6-1.6v-1.8c0-.88.72-1.6 1.6-1.6h1.87q.047-.136.106-.269l.034-.081l-1.32-1.32c-.62-.62-.62-1.64 0-2.26l1.27-1.27c.62-.62 1.64-.62 2.26 0l1.32 1.32q.096-.043.2-.08l.15-.06V3.6c0-.88.72-1.6 1.6-1.6h1.8c.88 0 1.6.72 1.6 1.6v1.87q.136.047.269.106l.081.034l1.32-1.32c.62-.62 1.64-.62 2.26 0l1.27 1.27c.62.62.62 1.64 0 2.26l-1.32 1.32q.043.096.08.2zM16 12a4 4 0 1 1-8 0a4 4 0 0 1 8 0" clip-rule="evenodd" />
  <path fill="currentColor" d="m18.51 9.49l-.692.288a.75.75 0 0 0 .692.462zm-.14-.35l-.53-.53a.75.75 0 0 0-.153.84zm1.32-1.32l-.53-.53zm0-2.26l-.53.53zm-1.27-1.27l.53-.53zm-2.26 0l-.53-.53zm-1.32 1.32l-.288.692a.75.75 0 0 0 .818-.162zm-.35-.14h-.75c0 .323.207.61.513.712zm-5 0l.288.692a.75.75 0 0 0 .462-.692zm-.35.14l-.53.53a.75.75 0 0 0 .84.153zM7.82 4.29l.53-.53zm-2.26 0l-.53-.53zM4.29 5.56l.53.53zm0 2.26l.53-.53zm1.32 1.32l.692.288a.75.75 0 0 0-.162-.818zm-.14.35v.75a.75.75 0 0 0 .712-.513zm0 5l.692-.289a.75.75 0 0 0-.692-.461zm.14.35l.53.53a.75.75 0 0 0 .153-.84zm-1.32 1.32l-.53-.53zm0 2.26l.53-.53zm1.27 1.27l-.53.53zm2.26 0l.53.53zm1.32-1.32l.288-.692a.75.75 0 0 0-.818.162zm.35.14h.75a.75.75 0 0 0-.513-.712zm5 0l-.289-.692a.75.75 0 0 0-.461.692zm.35-.14l.53-.53a.75.75 0 0 0-.84-.153zm1.32 1.32l-.53.53zm2.26 0l.53.53zm1.27-1.27l-.53-.53zm-1.32-3.58l-.692-.288a.75.75 0 0 0 .162.818zm.14-.35v-.75a.75.75 0 0 0-.712.513zm1.87-5.75h-1.87v1.5h1.87zm-1.178.462c-.02-.048-.037-.093-.062-.159a4 4 0 0 0-.087-.213l-1.366.62c.015.033.03.072.053.13c.02.051.047.126.078.198zm-.302.468l1.32-1.32l-1.06-1.06l-1.32 1.32zm1.32-1.32a2.355 2.355 0 0 0 0-3.32l-1.06 1.06a.856.856 0 0 1 0 1.2zm0-3.32l-1.27-1.27l-1.06 1.06l1.27 1.27zm-1.27-1.27a2.355 2.355 0 0 0-3.32 0l1.06 1.06a.856.856 0 0 1 1.2 0zm-3.32 0l-1.32 1.32l1.06 1.06l1.32-1.32zm-.502 1.158c-.091-.039-.246-.108-.4-.16l-.475 1.424c.085.028.15.059.299.12zm.112.552V3.6h-1.5v1.87zm0-1.87a2.355 2.355 0 0 0-2.35-2.35v1.5c.466 0 .85.384.85.85zm-2.35-2.35h-1.8v1.5h1.8zm-1.8 0A2.355 2.355 0 0 0 8.74 3.6h1.5c0-.466.384-.85.85-.85zM8.74 3.6v1.87h1.5V3.6zm.462 1.178c-.048.02-.093.037-.159.062c-.06.022-.136.052-.213.087l.62 1.366c.033-.015.072-.03.13-.053c.051-.02.126-.047.198-.078zm.468.302L8.35 3.76L7.29 4.82l1.32 1.32zM8.35 3.76a2.355 2.355 0 0 0-3.32 0l1.06 1.06a.856.856 0 0 1 1.2 0zm-3.32 0L3.76 5.03l1.06 1.06l1.27-1.27zM3.76 5.03a2.355 2.355 0 0 0 0 3.32l1.06-1.06a.856.856 0 0 1 0-1.2zm0 3.32l1.32 1.32l1.06-1.06l-1.32-1.32zm1.158.502c-.039.091-.108.246-.16.4l1.424.475c.028-.085.059-.15.12-.299zm.552-.112H3.6v1.5h1.87zm-1.87 0a2.355 2.355 0 0 0-2.35 2.35h1.5c0-.466.384-.85.85-.85zm-2.35 2.35v1.8h1.5v-1.8zm0 1.8a2.355 2.355 0 0 0 2.35 2.35v-1.5a.855.855 0 0 1-.85-.85zm2.35 2.35h1.87v-1.5H3.6zm1.178-.462c.02.047.037.093.062.159c.022.06.052.136.087.213l1.366-.62a2 2 0 0 1-.053-.13c-.02-.051-.047-.126-.078-.199zm.302-.468l-1.32 1.32l1.06 1.06l1.32-1.32zm-1.32 1.32a2.355 2.355 0 0 0 0 3.32l1.06-1.06a.856.856 0 0 1 0-1.2zm0 3.32l1.27 1.27l1.06-1.06l-1.27-1.27zm1.27 1.27a2.355 2.355 0 0 0 3.32 0l-1.06-1.06a.856.856 0 0 1-1.2 0zm3.32 0l1.32-1.32l-1.06-1.06l-1.32 1.32zm.502-1.158c.091.039.246.108.4.16l.475-1.424c-.085-.028-.15-.059-.299-.12zm-.112-.552v1.87h1.5v-1.87zm0 1.87a2.355 2.355 0 0 0 2.35 2.35v-1.5a.855.855 0 0 1-.85-.85zm2.35 2.35h1.8v-1.5h-1.8zm1.8 0a2.355 2.355 0 0 0 2.35-2.35h-1.5c0 .466-.384.85-.85.85zm2.35-2.35v-1.87h-1.5v1.87zm-.462-1.178c.047-.02.093-.037.159-.062c.06-.023.136-.052.213-.087l-.62-1.366c-.033.015-.072.03-.13.053c-.051.02-.126.047-.199.078zm-.468-.302l1.32 1.32l1.06-1.06l-1.32-1.32zm1.32 1.32a2.355 2.355 0 0 0 3.32 0l-1.06-1.06a.856.856 0 0 1-1.2 0zm3.32 0l1.27-1.27l-1.06-1.06l-1.27 1.27zm1.27-1.27a2.355 2.355 0 0 0 0-3.32l-1.06 1.06a.856.856 0 0 1 0 1.2zm0-3.32l-1.32-1.32l-1.06 1.06l1.32 1.32zm-1.158-.502c.039-.091.108-.246.16-.4l-1.424-.475c-.028.085-.059.15-.12.299zm-.552.112h1.87v-1.5h-1.87zm1.87 0a2.355 2.355 0 0 0 2.35-2.35h-1.5c0 .466-.384.85-.85.85zm2.35-2.35v-1.8h-1.5v1.8zm0-1.8a2.355 2.355 0 0 0-2.35-2.35v1.5c.466 0 .85.384.85.85zm-7.48.91A3.25 3.25 0 0 1 12 15.25v1.5A4.75 4.75 0 0 0 16.75 12zM12 15.25A3.25 3.25 0 0 1 8.75 12h-1.5A4.75 4.75 0 0 0 12 16.75zM8.75 12A3.25 3.25 0 0 1 12 8.75v-1.5A4.75 4.75 0 0 0 7.25 12zM12 8.75A3.25 3.25 0 0 1 15.25 12h1.5A4.75 4.75 0 0 0 12 7.25z" />
</svg>"""

  [<Literal>]
  let FileFolder =
    """<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 16 16">
  <path fill="currentColor" d="M13.5 5.88c-.28 0-.5-.22-.5-.5V1.5c0-.28-.22-.5-.5-.5h-9c-.28 0-.5.22-.5.5v2c0 .28-.22.5-.5.5S2 3.78 2 3.5v-2C2 .67 2.67 0 3.5 0h9c.83 0 1.5.67 1.5 1.5v3.88c0 .28-.22.5-.5.5" />
  <path fill="currentColor" d="M14.5 16h-13C.67 16 0 15.33 0 14.5v-10C0 3.67.67 3 1.5 3h4.75c.16 0 .31.07.4.2L8 5h6.5c.83 0 1.5.67 1.5 1.5v8c0 .83-.67 1.5-1.5 1.5M1.5 4c-.28 0-.5.22-.5.5v10c0 .28.22.5.5.5h13c.28 0 .5-.22.5-.5v-8c0-.28-.22-.5-.5-.5H7.75a.48.48 0 0 1-.4-.2L6 4z" />
  <path fill="currentColor" d="M5.5 13h-2c-.28 0-.5-.22-.5-.5s.22-.5.5-.5h2c.28 0 .5.22.5.5s-.22.5-.5.5" />
</svg>"""

  [<Literal>]
  let House =
    """<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 48 48">
  <g fill="none" stroke="currentColor" stroke-linejoin="round" stroke-width="4">
      <path d="M44 44V20L24 4L4 20v24h12V26h16v18z" />
      <path stroke-linecap="round" d="M24 44V34" />
  </g>
</svg>"""

  [<Literal>]
  let MenuClosed =
    """<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 24 24">
  <path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6h10M4 12h16M7 12h13M4 18h10" />
</svg>"""

  [<Literal>]
  let MenuOpen =
    """<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 24 24">
  <path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M7 12h13m-10 6h10" />
</svg>"""

  [<Literal>]
  let Inbox =
    """<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 16 16">
  <path fill="currentColor" fill-rule="evenodd" d="M1.5 14h13l.5-.5V9l-2.77-7.66l-.47-.34H4.27l-.47.33L1 8.74v4.76zM14 13H2v-2.98h2.55l.74 1.25l.43.24h4.57l.44-.26l.69-1.23H14zm-.022-3.98H11.12l-.43.26l-.69 1.23H6.01l-.75-1.25l-.43-.24H2V9l2.62-7h6.78z" clip-rule="evenodd" />
</svg>"""

  [<Literal>]
  let Processor =
    """<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 48 48">
  <g fill="currentColor">
      <path d="m31 8l11.9 1.6l-9.8 9.8zM17 40L5.1 38.4l9.8-9.8zM8 17L9.6 5.1l9.8 9.8z" />
      <path d="m9.3 21.2l-4.2.8c-.1.7-.1 1.3-.1 2c0 4.6 1.6 9 4.6 12.4l3-2.6C10.3 31.1 9 27.6 9 24c0-.9.1-1.9.3-2.8M24 5c-5.4 0-10.2 2.3-13.7 5.9l2.8 2.8C15.9 10.8 19.7 9 24 9c.9 0 1.9.1 2.8.3l.7-3.9C26.4 5.1 25.2 5 24 5m14.7 21.8l4.2-.8c.1-.7.1-1.3.1-2c0-4.4-1.5-8.7-4.3-12.1l-3.1 2.5c2.2 2.7 3.4 6.1 3.4 9.5c0 1-.1 2-.3 2.9m-3.8 7.5C32.1 37.2 28.3 39 24 39c-.9 0-1.9-.1-2.8-.3l-.7 3.9c1.2.2 2.4.3 3.5.3c5.4 0 10.2-2.3 13.7-5.9z" />
      <path d="m40 31l-1.6 11.9l-9.8-9.8z" />
  </g>
</svg>"""

  [<Literal>]
  let Bell =
    """<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 24 24">
  <path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M8 18H3a1 1 0 0 1-.894-1.447L4 12.763V10a8 8 0 1 1 16 0v2.764l1.894 3.789A1 1 0 0 1 21 18h-5m-8 0h8m-8 0a4 4 0 0 0 8 0" />
</svg>"""

  [<Literal>]
  let Done =
    """<svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 24 24">
  <g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" color="currentColor">
      <path d="M13.5 20s1 0 2 2c0 0 3.177-5 6-6M7 16h4m-4-5h8M6.5 3.5c-1.556.047-2.483.22-3.125.862c-.879.88-.879 2.295-.879 5.126v6.506c0 2.832 0 4.247.879 5.127C4.253 22 5.668 22 8.496 22h2.5m4.496-18.5c1.556.047 2.484.22 3.125.862c.88.88.88 2.295.88 5.126V13.5" />
      <path d="M6.496 3.75c0-.966.784-1.75 1.75-1.75h5.5a1.75 1.75 0 1 1 0 3.5h-5.5a1.75 1.75 0 0 1-1.75-1.75" />
  </g>
</svg>"""

module FS =
  let openFolder = Html.i [ prop.dangerouslySetInnerHTML SVG.OpenFolder ]
  let closeFolder = Html.i [ prop.dangerouslySetInnerHTML SVG.CloseFolder ]
  let createFolder = Html.i [ prop.dangerouslySetInnerHTML SVG.CreateFolder ]
  let deleteFolder = Html.i [ prop.dangerouslySetInnerHTML SVG.DeleteFolder ]

  let createFile = Html.i [ prop.dangerouslySetInnerHTML SVG.AddFile ]
  let removeFile = Html.i [ prop.dangerouslySetInnerHTML SVG.RemoveFile ]
  let file = Html.i [ prop.dangerouslySetInnerHTML SVG.File ]

let house = Html.i [ prop.dangerouslySetInnerHTML SVG.House ]
let nextAction = Html.i [ prop.dangerouslySetInnerHTML SVG.Done ]
let menuClosed = Html.i [ prop.dangerouslySetInnerHTML SVG.MenuClosed ]
let menuOpen = Html.i [ prop.dangerouslySetInnerHTML SVG.MenuOpen ]
let bell = Html.i [ prop.dangerouslySetInnerHTML SVG.Bell ]
let inbox = Html.i [ prop.dangerouslySetInnerHTML SVG.Inbox ]
let processor = Html.i [ prop.dangerouslySetInnerHTML SVG.Processor ]
let fileFolder = Html.i [ prop.dangerouslySetInnerHTML SVG.FileFolder ]
let settings = Html.i [ prop.dangerouslySetInnerHTML SVG.Settings ]
let help = Html.i [ prop.dangerouslySetInnerHTML SVG.Help ]
