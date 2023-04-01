/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
      './wwwroot/index.html', 
      './wwwroot/scripts/*.js',
      './Pages/*.razor', 
      './Pages/**/*.razor',
      './Shared/*.razor',
      './Shared/**/*.razor'
  ],
  theme: {
    extend: {
        colors: {
            discord: '#404EED'
        }
    },
  },
  plugins: [],
}
