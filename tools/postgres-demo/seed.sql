drop table if exists search_documents;

create table search_documents
(
    id integer not null primary key,
    text text not null
);

insert into search_documents (id, text)
values
    (1, 'Иванов Сергей Петрович'),
    (2, 'Папандопуло Александр'),
    (3, 'Красный велосипед');